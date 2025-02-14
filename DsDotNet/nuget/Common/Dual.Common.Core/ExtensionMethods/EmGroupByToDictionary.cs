using System;
using System.Collections.Generic;
using System.Linq;

namespace Dual.Common.Core
{
    public static class EmGroupByToDictionary
    {
        // https://stackoverflow.com/questions/6361880/linq-group-by-into-a-dictionary-object
        /// <summary>GroupBy 수행 결과를 dictionary 에 저장</summary>
        public static Dictionary<K, V[]> GroupByToDictionary<K, V>(this IEnumerable<V> xs, Func<V, K> keySelector)
        {
            return xs.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToArray());
        }


        // DistinctBy : https://stackoverflow.com/questions/2537823/distinct-by-property-of-class-with-linq
        public static IEnumerable<T> KeyDistinctedBy<T, V>(this IEnumerable<T> xs, Func<T, V> keySelector) =>
            xs.GroupBy(keySelector).Select(g => g.First())
            ;

        public static (K, V) ToTuple<K, V>(this KeyValuePair<K, V> pr) => (pr.Key, pr.Value);

        public static string UniqueId() => Guid.NewGuid().ToString().Substring(0, 8);
    }
}

