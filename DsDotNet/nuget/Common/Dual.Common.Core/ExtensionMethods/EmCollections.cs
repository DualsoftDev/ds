using Dual.Common.Base.CS;
using Dual.Common.Base.FS;

using System;
using System.Collections.Generic;

namespace Dual.Common.Core
{
    public static class EmCollections
	{
		public static IEnumerable<T> ToEnumerable<T>(this Array arr)
		{
			if (arr.IsNullOrEmpty())
				yield break;

			foreach (object o in arr)
				yield return (T) o;
		}

        /// <summary>
        /// F# Seq.cache 사용
        /// </summary>
        public static IEnumerable<X> Cache<X>(this IEnumerable<X> xs) => EmCollection.Cache(xs);


        ///// <summary>
        ///// F# Array.map 사용
        ///// </summary>
        //public static Y[] Map<X, Y>(this X[] xs, Func<X, Y> selector) => EmCollection.Map(xs, selector);


        /// <summary>
        /// F# Array.collect 사용
        /// </summary>
        public static Y[] Bind<X, Y>(this X[] xs, Func<X, Y[]> selector) => EmCollection.Bind(xs, selector);
        /// <summary>
        /// F# Array.collect with index 사용
        /// </summary>
        public static Y[] Bind<X, Y>(this X[] xs, Func<X, int, Y[]> selector) => EmCollection.Bindi(xs, selector);

    }
}
