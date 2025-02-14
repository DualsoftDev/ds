using System;
using System.Reactive.Linq;

namespace Dsu.Common.Utilities
{
    public static class EmRx
    {
        /// <summary>
        /// Throttle 을 사용하는 경우, event 가 time slice 내에서 계속 들어오면, 모든 event 가 throttle 된다.
        /// OnceInTimeWindow 는 Time slice 내에서 적어도 한번은 trigger 하기 위한 extension method.
        /// http://stackoverflow.com/questions/7999503/rx-how-can-i-respond-immediately-and-throttle-subsequent-requests
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static IObservable<T> OnceInTimeWindow<T>(this IObservable<T> source, TimeSpan ts)
        {
            return source.Window(() => Observable.Interval(ts))
                .SelectMany(x => x.Take(1));
        }
    }
}
