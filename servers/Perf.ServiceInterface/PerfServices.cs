using System.Threading;
using ServiceStack;

namespace Perf.SeviceInterface
{
    public class SpinWait : IReturn<SpinWait>
    {
        public int? Iterations { get; set; }
    }

    public class Sleep : IReturn<Sleep>
    {
        public int? ForMs { get; set; }
    }

    public class PerfServices : Service
    {
        private const int DefaultIterations = 1000 * 1000;
        private const int DefaultMs = 100;

        public object Any(SpinWait request)
        {
            Thread.SpinWait(request.Iterations.GetValueOrDefault(DefaultIterations));
            return request;
        }

        public object Any(Sleep request)
        {
            Thread.Sleep(request.ForMs.GetValueOrDefault(DefaultMs));
            return request;
        }
    }
}
