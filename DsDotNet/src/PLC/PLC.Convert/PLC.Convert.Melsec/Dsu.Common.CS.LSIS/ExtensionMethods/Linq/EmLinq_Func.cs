using System;
using System.Collections.Generic;

namespace Dsu.Common.CS.LSIS.Core.ExtensionMethods
{
    public static partial class EmLinq
    {
        /// <summary>
        /// 두개의 function 을 합성
        /// Real world functional progarmming.pdf, pp.162
        /// </summary>
        public static Func<A, C> Compose<A, B, C>(this Func<A, B> f, Func<B, C> g)
        {
            return (x) => g(f(x));
        }

        //https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
