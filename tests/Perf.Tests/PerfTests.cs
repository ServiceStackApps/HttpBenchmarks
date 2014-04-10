using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace Perf.Tests
{
    [TestFixture]
    public class PerfTests
    {
        [Test]
        public void Benchmark_AspNet()
        {
            "ASP.NET".Print();
            Benchmark("http://localhost:55000/");

            "HttpListener".Print();
            Benchmark("http://localhost:55001/");

            "HttpListenerPool".Print();
            Benchmark("http://localhost:55002/");

            "HttpListenerSmartPool".Print();
            Benchmark("http://localhost:55003/");
        }

        public void Benchmark(string baseUrl)
        {
            MeasureUrl(baseUrl.AppendPath("json"));
            MeasureUrl(baseUrl.AppendPath("db"));
            MeasureUrl(baseUrl.AppendPath("queries"));
            MeasureUrl(baseUrl.AppendPath("fortunes"));
            MeasureUrl(baseUrl.AppendPath("updates"));
            MeasureUrl(baseUrl.AppendPath("plaintext"));
        }

        public void MeasureUrl(string url)
        {
            var errors = new List<Exception>();
            var bytesDownloaded = 0;
            var microSecs = PerfUtils.Measure(() =>
                {
                    try
                    {
                        var download = url.GetBytesFromUrl();
                        bytesDownloaded += download.Length;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                },
                times: 1,
                runForMs: 2000);

            if (errors.Count > 0)
                errors.Map(x => x.Message).ToHashSet().PrintDump();

            "{0} took avg of {1}us with and had {2} errors ({3} bytes downloaded)"
                .Print(url, microSecs, errors.Count, bytesDownloaded);
        }

    }
}

