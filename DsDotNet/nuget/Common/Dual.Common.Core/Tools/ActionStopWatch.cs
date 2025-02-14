using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    // see duration on F# side
    public static class ActionStopWatch
    {
        /// <summary>
        /// 주어진 action 을 수행하고, 이때 걸린 시간을 반환한다.
        /// http://stackoverflow.com/questions/969290/exact-time-measurement-for-performance-testing
        /// </summary>
        public static TimeSpan GetExecutionTimeSpan(this Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        public static void Measure(this Action action, Action<TimeSpan> trace)
        {
#if true
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            trace(stopwatch.Elapsed);
#else
            action();
#endif
        }

        public static async Task<TimeSpan> MeasureAsync(this Task task, Action<TimeSpan> trace)
        {
#if true
            Stopwatch stopwatch = Stopwatch.StartNew();
            await task;
            stopwatch.Stop();
            trace(stopwatch.Elapsed);
#else
            await task;
#endif

            return stopwatch.Elapsed;
        }
    }

    /* SAMPLE USAGE
     * 
        ActionStopWatch.Measure(
            () => 
            {
                // Actions, here
            },
            (span) =>
            {
                logger?.InfoFormat("sensor vs BB intersection took {0} milliseconds.", span.Milliseconds);
            });
     */
}
