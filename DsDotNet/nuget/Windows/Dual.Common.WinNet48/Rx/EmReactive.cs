using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Dsu.Common.Utilities.ExtensionMethods
{
    public static class EmReactive
    {
        /// <summary>
        /// Rx HOL.NET.pdf
        /// http://stackoverflow.com/questions/13915109/removetimestamp-rx-removed-but-not-forgotten
        /// </summary>
        /// <param name="source"></param>
        /// <param name="onNext"></param>
        /// <returns></returns>
        public static IObservable<T> LogTimestampedValues<T>(this IObservable<T> source, Action<Timestamped<T>> onNext)
        {
            return source.Timestamp().Do(onNext).Select(t => t.Value);
        }
    }
}
