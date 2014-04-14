﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;
using ResultsView.ServiceModel;
using ResultsView.ServiceModel.Types;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.OrmLite;

namespace ResultsView.ServiceInterface
{
    [Authenticate]
    public class AuthenticatedServices : Service
    {
        public IUserAuthRepository UserAuthRepository { get; set; }

        private int GetSignedInUserId()
        {
            var authSession = base.GetSession();

            if (authSession == null || authSession.UserAuthId == null)
                throw new UnauthorizedAccessException("Must be signed in to use this service");

            return int.Parse(authSession.UserAuthId);
        }

        public UserInfo Get(MyInfo request)
        {
            var userId = GetSignedInUserId();
            var userAuth = UserAuthRepository.GetUserAuth(userId.ToString());
            var userInfo = userAuth.ConvertTo<UserInfo>();

            userInfo.UserAuthId = userId;

            if (userInfo.DisplayName == null)
                userInfo.DisplayName = userAuth.UserName
                    ?? userAuth.FullName
                    ?? "{0} {1}".Fmt(userAuth.FirstName, userAuth.LastName);

            if (userInfo.ProfileUrl64 == null)
                userInfo.ProfileUrl64 = this.SessionAs<UserSession>().ProfileUrl64
                    ?? "/Content/img/no-profile-pic-64.png";

            return userInfo;
        }

        public object Get(FindTestPlans request)
        {
            var userId = GetSignedInUserId();
            return Db.Select<TestPlan>(q => q.UserAuthId == userId)
                .OrderByDescending(x => x.Id);
        }

        public object Get(FindTestRuns request)
        {
            var userId = GetSignedInUserId();
            var planId = request.TestPlanId;
            var testRuns = Db.Select<TestRun>(q =>
                q.UserAuthId == userId && q.TestPlanId == planId)
                .OrderByDescending(x => x.Id);

            var testRunCounts = Db.Dictionary<int, int>(
                Db.From<TestResult>()
                .Select(x => new { x.TestRunId, count = Sql.Count("*") })
                .Where(q => q.UserAuthId == userId && q.TestPlanId == planId)
                .GroupBy(x => x.TestRunId));

            testRuns
                .Where(x => testRunCounts.ContainsKey(x.Id))
                .Each(x => x.TestResultsCount = testRunCounts[x.Id]);

            return testRuns;
        }

        public object Post(CreateTestPlan request)
        {
            var plan = request.ConvertTo<TestPlan>();
            plan.CreatedDate = DateTime.UtcNow;

            if (plan.Name.IsNullOrEmpty())
                throw new ArgumentNullException("Name");

            if (plan.Slug.IsNullOrEmpty())
                plan.Slug = plan.Name.SafeVarName().ToLower();

            if (Db.Count<TestPlan>(q => q.Slug == plan.Slug) > 0)
                throw new ArgumentException("Slug already exists", "Slug");

            plan.UserAuthId = GetSignedInUserId();

            Db.Save(plan);

            return HttpResult.SoftRedirect(new EditTestPlan { Id = plan.Id }.ToGetUrl(), plan);
        }

        public object Any(UpdateTestPlanLabels request)
        {
            var testPlan = GetTestPlan(request.Id);
            
            testPlan.ServerLabels = request.ServerLabels.ParseKeyValueText(delimiter:" ");
            testPlan.TestLabels = request.TestLabels.ParseKeyValueText(delimiter: " ");
            
            Db.Save(testPlan);
            
            return AddMissingLabels(testPlan);
        }

        public object Any(DeleteTestPlan request)
        {
            var testPlan = GetTestPlan(request.Id);

            using (var trans = Db.OpenTransaction())
            {
                var userId = GetSignedInUserId();
                Db.Delete<TestResult>(q => q.UserAuthId == userId && q.TestPlanId == testPlan.Id);
                Db.Delete<TestRun>(q => q.UserAuthId == userId && q.TestPlanId == testPlan.Id);
                Db.Delete<TestPlan>(q => q.UserAuthId == userId && q.Id == testPlan.Id);

                trans.Commit();                
            }

            return HttpResult.SoftRedirect("/");
        }

        private TestPlan GetTestPlan(int testPlanId)
        {
            var testPlan = Db.SingleById<TestPlan>(testPlanId);
            if (testPlan == null)
                throw HttpError.NotFound("Test Plan {0} does not exist".Fmt(testPlanId));

            return testPlan;
        }

        //[AddHeader(ContentType = MimeTypes.Html)]
        public object Any(EditTestPlan request)
        {
            //If no test run exists, create one then redirect to it
            var testRun = (request.TestRunId != null
                    ? Db.SingleById<TestRun>(request.TestRunId.Value)
                    : null)
                ?? GetLatestOrCreateTestRun(GetSignedInUserId(), request.Id);

            if (request.TestRunId == null)
            {
                request.TestRunId = testRun.Id;
                return HttpResult.Redirect(request.ToGetUrl());
            }

            using (var service = ResolveService<WebServices>())
            {
                var testPlan = service.Any(request.ConvertTo<GetTestPlan>());
                var response = AddMissingLabels(testPlan).ConvertTo<EditTestPlanResponse>();
                response.TestRunId = request.TestRunId;

                return response;
            }
        }

        private TestPlan AddMissingLabels(TestPlan testPlan)
        {
            var distinctResults = Db.Select(Db.From<TestResult>()
                .SelectDistinct(x => new { x.Hostname, x.Port, x.RequestPath })
                .Where(q => q.TestPlanId == testPlan.Id));

            if (testPlan.ServerLabels == null)
                testPlan.ServerLabels = new Dictionary<string, string>();

            if (testPlan.TestLabels == null)
                testPlan.TestLabels = new Dictionary<string, string>();

            var serverLabels = distinctResults.ConvertAll(x => x.Hostname + ":" + x.Port).ToHashSet();
            serverLabels.Each(x => {
                if (!testPlan.ServerLabels.ContainsKey(x))
                    testPlan.ServerLabels[x] = "";
            });

            var testLabels = distinctResults.ConvertAll(x => x.RequestPath).ToHashSet();
            testLabels.Each(x =>
            {
                if (!testPlan.TestLabels.ContainsKey(x))
                    testPlan.TestLabels[x] = "";
            });

            return testPlan;
        }

        public object Post(CreateTestRun request)
        {
            var testRun = CreateTestRun(request.TestPlanId, request.SeriesId);

            return HttpResult.SoftRedirect(
                new EditTestPlan { Id = request.TestPlanId, TestRunId = testRun.Id }.ToGetUrl(),
                testRun);
        }

        public object Any(DeleteTestRun request)
        {
            var testRun = Db.SingleById<TestRun>(request.Id);
            if (testRun == null)
                throw HttpError.NotFound("Test Run {0} does not exist".Fmt(request.Id));

            using (var trans = Db.OpenTransaction())
            {
                var userId = GetSignedInUserId();
                Db.Delete<TestResult>(q => q.UserAuthId == userId && q.TestRunId == request.Id);
                Db.Delete<TestRun>(q => q.UserAuthId == userId && q.Id == request.Id);

                trans.Commit();
            }

            return HttpResult.SoftRedirect(new EditTestPlan { Id = testRun.TestPlanId }.ToGetUrl());
        }

        public object Post(AddTestResults request)
        {
            var testPlanId = request.TestPlanId;
            var testRunId = request.TestRunId.GetValueOrDefault();

            if (request.Contents.IsNullOrEmpty())
                throw new ArgumentNullException("Contents");

            var testResult = request.Contents.ToTestResult();

            if (testResult.Software.IsNullOrEmpty())
                throw new ArgumentException("Invalid Apache Benchmark File", "Contents");

            var results = new[] { testResult }.ToList();

            var testRun = AddTestResults(GetSignedInUserId(), testPlanId, testRunId, results);

            return new AddTestResultsResponse
            {
                TestRun = testRun,
                Results = results,
            };
        }

        public object Post(UploadTestResults request)
        {
            var testPlanId = request.TestPlanId;
            var testRunId = request.TestRunId.GetValueOrDefault();
            TestRun testRun = null;

            if (base.Request.Files.Length == 0)
                throw new ArgumentException("No Test Result supplied");

            var processedResults = new List<TestResult>();
            var newResults = new List<TestResult>();
            foreach (var httpFile in base.Request.Files)
            {
                if (httpFile.FileName.ToLower().EndsWith(".zip"))
                {
                    using (var zip = ZipFile.Read(httpFile.InputStream))
                    {
                        var zipResults = new List<TestResult>();
                        foreach (var zipEntry in zip)
                        {
                            using (var ms = new MemoryStream())
                            {
                                zipEntry.Extract(ms);
                                var bytes = ms.ToArray();

                                var result = new MemoryStream(bytes).ToTestResult();
                                zipResults.Add(result);
                            }
                        }

                        if (request.CreateNewTestRuns)
                        {
                            var seriesId = httpFile.FileName.Substring(0, httpFile.FileName.Length - ".zip".Length);
                            testRun = CreateTestRun(testPlanId, seriesId);
                            testRun = AddTestResults(GetSignedInUserId(), testPlanId, testRun.Id, zipResults);
                            processedResults.AddRange(zipResults);
                        }
                        else
                        {
                            newResults.AddRange(zipResults);
                        }
                    }
                }
                else
                {
                    var result = httpFile.InputStream.ToTestResult();
                    newResults.Add(result);
                }
            }

            if (newResults.Count > 0)
            {
                testRun = AddTestResults(GetSignedInUserId(), testPlanId, testRunId, newResults);
                processedResults.AddRange(newResults);
            }

            return new UploadTestResultsResponse
            {
                success = true,
                TestRun = testRun,
                Results = processedResults,
            };
        }

        private TestRun AddTestResults(int userId, int testPlanId, int testRunId, List<TestResult> testResults)
        {
            if (testPlanId == default(int))
                throw new ArgumentNullException("TestPlanId");

            var testPlan = Db.SingleById<TestPlan>(testPlanId);
            if (testPlan == null)
                throw HttpError.NotFound("Test Plan {0} does not exist".Fmt(testPlanId));

            if (testPlan.UserAuthId != userId)
                throw HttpError.Unauthorized("You do not have permission to write to this plan");

            TestRun testRun = null;
            if (testRunId > 0)
            {
                testRun = Db.Single<TestRun>(q =>
                    q.Id == testRunId
                    && q.UserAuthId == userId);

                if (testRun == null)
                    throw HttpError.NotFound("Test Run {0} does not exist".Fmt(testRunId));
            }
            else
            {
                testRun = GetLatestOrCreateTestRun(userId, testPlanId);
            }

            testResults.RemoveAll(x => x.Hostname == null || x.Port == 0); //clear bogus records
            testResults.Each(x =>
            {
                x.UserAuthId = userId;
                x.TestPlanId = testPlanId;
                x.TestRunId = testRun.Id;
            });

            Db.SaveAll(testResults);

            return testRun;
        }

        private TestRun GetLatestOrCreateTestRun(int userId, int testPlanId)
        {
            var testRun = Db.Select<TestRun>(q =>
                q.Where(
                    x => x.UserAuthId == userId
                        && x.TestPlanId == testPlanId)
                .OrderByDescending(x => x.Id))
                .FirstOrDefault();

            return testRun ?? CreateTestRun(testPlanId);
        }

        private TestRun CreateTestRun(int planId, string seriesId = null)
        {
            var testRun = new TestRun
            {
                UserAuthId = GetSignedInUserId(),
                TestPlanId = planId,
                SeriesId = seriesId ?? "Created on {0}".Fmt(DateTime.UtcNow.ToString("yyyy-MM-dd")),
                CreatedDate = DateTime.UtcNow,
            };

            Db.Save(testRun);

            return testRun;
        }
    }
}