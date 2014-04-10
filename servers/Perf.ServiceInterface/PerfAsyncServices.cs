using System.Threading;
using System.Threading.Tasks;
using ServiceStack;

namespace Perf.SeviceInterface
{
    public class SpinWaitAsync : IReturn<SpinWait>
    {
        public int? Iterations { get; set; }
    }

    public class SleepAsync : IReturn<SleepAsync>
    {
        public int? ForMs { get; set; }
    }

    public class PerfAsyncServices : Service
    {
        private const int DefaultIterations = 1000 * 1000;
        private const int DefaultMs = 100;

        public Task Any(SpinWaitAsync request)
        {
            return new Task<SpinWaitAsync>(
                () => {
                    Thread.SpinWait(request.Iterations.GetValueOrDefault(DefaultIterations));
                    return request;
                });
        }

        public async Task<SleepAsync> Any(SleepAsync request)
        {
            await Task.Delay(request.ForMs.GetValueOrDefault(DefaultMs));
            return request;
        }
    }
}
