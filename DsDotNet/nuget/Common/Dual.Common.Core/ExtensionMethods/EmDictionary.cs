using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Dual.Common.Core
{
    public static class EmDictionary
    {
        /// <summary>
        /// K->V dictionary 를 V->K dictionary 로 변환
        /// </summary>
        public static Dictionary<V,K> SwapKeyValue<K,V>(this Dictionary<K,V> dict)
        {
            if (dict.Values.Count != dict.Values.Distinct().Count())
                throw new Exception($"Values are not distinct!");

            return dict.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

        /// <summary>
        /// K->V ConcurrentDictionary 를 V->K dictionary 로 변환
        /// </summary>
        public static Dictionary<V, K> SwapKeyValue<K, V>(this ConcurrentDictionary<K, V> dict)
        {
            if (dict.Values.Count != dict.Values.Distinct().Count())
                throw new Exception($"Values are not distinct!");

            return dict.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

    }
}
