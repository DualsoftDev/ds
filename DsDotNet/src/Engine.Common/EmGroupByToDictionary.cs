using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Common
{
    public static class EmGroupByToDictionary
    {
        // https://stackoverflow.com/questions/6361880/linq-group-by-into-a-dictionary-object
        public static Dictionary<K, V[]> GroupByToDictionary<K, V>(this IEnumerable<V> xs, Func<V, K> keySelector)
        {
            return xs.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToArray());
        }
    }
}
