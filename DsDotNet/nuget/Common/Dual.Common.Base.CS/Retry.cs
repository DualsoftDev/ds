using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Base.CS
{
    /// <summary>
    /// Light-weighted retry utility.   For full-blown retry functionality, consider using Polly or something.
    /// </summary>
    public static class Retrial

    {
        public static IEnumerable<TimeSpan> GetInfiniteTimeSpans(double intervalSec)
        {
            while (true)
                yield return TimeSpan.FromSeconds(intervalSec);
        }
        /// <summary>
        /// 10ms : 1/100 sec
        /// </summary>
        public static IEnumerable<TimeSpan> InfiniteCentiSeconds => GetInfiniteTimeSpans(0.01);
        /// <summary>
        /// 100ms : 1/10 sec
        /// </summary>
        public static IEnumerable<TimeSpan> InfiniteDeciSeconds => GetInfiniteTimeSpans(0.1);
        public static IEnumerable<TimeSpan> InfiniteSeconds => GetInfiniteTimeSpans(1);
        public static IEnumerable<TimeSpan> InfiniteMinutes => GetInfiniteTimeSpans(60);
        public static IEnumerable<TimeSpan> InfiniteHours => GetInfiniteTimeSpans(3600);

        /// <summary>
        /// 1 초 간격으로 5번, 분 간격으로 무한대
        /// </summary>
        public static IEnumerable<TimeSpan> PauseSet1 =>
            InfiniteSeconds.Take(5).Concat(InfiniteMinutes);
        public static IEnumerable<TimeSpan> PauseSet5 =>
            GetInfiniteTimeSpans(5).Take(5).Concat(InfiniteMinutes);

        /// <summary>
        /// Exception 발생 시, 해당 exception 들을 수집하는 handler action 과
        /// handler 에 의해서 수집된 exception 들을 tuple 로 반환
        /// </summary>
        public static (Action<Exception>, List<Exception>) CreateExceptionCollector()
        {
            var exceptions = new List<Exception>();
            var onException = new Action<Exception>(ex => exceptions.Add(ex));
            return (onException, exceptions);
        }


        public static async Task<T> Retry<T>(this Func<Task<T>> function) => await Retry(function, PauseSet1);
        public static async Task<T> Retry<T>(this Func<T> function) => await Retry(function, PauseSet1);
        public static async Task Retry(this Func<Task> action) => await Retry(action, PauseSet1);
        public static async Task Retry(this Action action) => await Retry(action, PauseSet1);

        public static async Task<T> Retry<T>(this Func<Task<T>> function, IEnumerable<TimeSpan> pauses, Action<Exception> onException=null)
        {
            foreach (var p in pauses)
            {
                try { return await function(); }
                catch (Exception ex) { onException?.Invoke(ex); }
                await Task.Delay(p);
            }
            return await function();
        }


        public static async Task<T> Retry<T>(this Func<T> function, IEnumerable<TimeSpan> pauses, Action<Exception> onException = null)
        {
            foreach (var p in pauses)
            {
                try { return function(); }
                catch (Exception ex) { onException?.Invoke(ex); }
                await Task.Delay(p);
            }
            return function();
        }



        public static async Task Retry(this Func<Task> action, IEnumerable<TimeSpan> pauses, Action<Exception> onException = null)
        {
            foreach (var p in pauses)
            {
                try { await action(); return; }
                catch (Exception ex) { onException?.Invoke(ex); }
                await Task.Delay(p);
            }
            await action();
        }


        public static async Task Retry(this Action action, IEnumerable<TimeSpan> pauses, Action<Exception> onException = null)
        {
            foreach (var p in pauses)
            {
                try { action(); return; }
                catch (Exception ex) { onException?.Invoke(ex); }
                await Task.Delay(p);
            }
            action();
        }

    }
}
