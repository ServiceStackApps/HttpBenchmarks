using BenchmarksAnalyzer.ServiceModel;
using BenchmarksAnalyzer.ServiceModel.Types;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;

namespace BenchmarksAnalyzer.ServiceInterface
{
    [RequiredRole(RoleNames.Admin)]
    public class AdminServices : Service
    {
        public object Any(Reset request)
        {
            Db.DropAndCreateTable<TestPlan>();
            Db.DropAndCreateTable<TestRun>();
            Db.DropAndCreateTable<TestResult>();
            Db.DropAndCreateTable<UserAuth>();
            Db.DropAndCreateTable<UserAuthDetails>();

            return "OK";
        }
    }
}