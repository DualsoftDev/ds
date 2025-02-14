using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

// Understanding map and apply
// http://fsharpforfunandprofit.com/posts/elevated-world/

namespace Dual.Common.Base.CS
{
	public static partial class EmLinq
	{
		/// <summary>
		/// Create IEnumerable from element
		/// http://stackoverflow.com/questions/1577822/passing-a-single-item-as-ienumerablet
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item"></param>
		/// <returns></returns>
		public static IEnumerable<T> ToEnumerable<T>(this T item) { yield return item; }
		public static IEnumerable<T> Return<T>(this T item) => item.ToEnumerable();
		public static IEnumerable<T> Yield<T>(this T item) => item.ToEnumerable();
		public static IEnumerable<T> ReturnEnumerable<T>(this T item) => item.ToEnumerable();
        public static IEnumerable<T> IfNullOrEmpty<T>(this IEnumerable<T> first, IEnumerable<T> second) => first.IsNullOrEmpty() ? second : first;
	    public static IEnumerable<T> CreateEmptySequence<T>() => Enumerable.Empty<T>();

        /// Generic 이 아닌 IEnumerable 을 Generic 으로 변환
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerable xs)
        {
            foreach (var item in xs)
                yield return (T)item;
        }

        public static IEnumerable<Y> Map<X, Y>(this IEnumerable<X> xs, Func<X, Y> selector) => xs.Select(selector);
		public static IEnumerable<Y> MapEnumerable<X, Y>(this IEnumerable<X> xs, Func<X, Y> selector) => xs.Select(selector);
		public static IEnumerable<Y> MapEnumerable<X, Y>(this Func<X, Y> selector, IEnumerable<X> xs) => xs.Select(selector);


		/// <summary>
		/// Reduce / Fold / Aggregate
		/// </summary>
		public static X Reduce<X>(this IEnumerable<X> xs, Func<X, X, X> func) => xs.Aggregate(func);
		public static TAccumulate Reduce<X, TAccumulate>(this IEnumerable<X> xs, TAccumulate seed, Func<TAccumulate, X, TAccumulate> func) => xs.Aggregate(seed, func);
		public static Y Reduce<X, TAccumulate, Y>(this IEnumerable<X> xs, TAccumulate seed, Func<TAccumulate, X, TAccumulate> func, Func<TAccumulate, Y> resultSelector) => xs.Aggregate(seed, func, resultSelector);
		public static X Fold<X>(this IEnumerable<X> xs, Func<X, X, X> func) => xs.Aggregate(func);
		public static TAccumulate Fold<X, TAccumulate>(this IEnumerable<X> xs, TAccumulate seed, Func<TAccumulate, X, TAccumulate> func) => xs.Aggregate(seed, func);
		public static Y Fold<X, TAccumulate, Y>(this IEnumerable<X> xs, TAccumulate seed, Func<TAccumulate, X, TAccumulate> func, Func<TAccumulate, Y> resultSelector) => xs.Aggregate(seed, func, resultSelector);


		/// <summary>
		/// Filter / Where
		/// </summary>
		public static IEnumerable<X> Filter<X>(this IEnumerable<X> xs, Func<X, bool> predicate) => xs.Where(predicate);

		/// <summary>
		/// Bind / FlatMap / Collect(F#) / SelectMany / Lift1
		/// </summary>
		public static IEnumerable<Y> Bind<X, Y>(this IEnumerable<X> xs, Func<X, IEnumerable<Y>> selector) => xs.SelectMany(selector);
		public static IEnumerable<Y> Bind<X, Y>(this IEnumerable<X> xs, Func<X, int, IEnumerable<Y>> selector) => xs.SelectMany(selector);

        public static IEnumerable<Y> Bind<X, TCollection, Y>(this IEnumerable<X> xs, Func<X, IEnumerable<TCollection>> collectionSelector, Func<X, TCollection, Y> resultSelector) => xs.SelectMany(collectionSelector, resultSelector);
		public static IEnumerable<Y> Bind<X, TCollection, Y>(this IEnumerable<X> xs, Func<X, int, IEnumerable<TCollection>> collectionSelector, Func<X, TCollection, Y> resultSelector) => xs.SelectMany(collectionSelector, resultSelector);


		public static IEnumerable<Y> Apply<X, Y>(this IEnumerable<Func<X, Y>> funcs, IEnumerable<X> xs)
		{
			return from f in funcs
				from s in xs
				select f(s);
		}


		public static IEnumerable<Y> Lift1<X, Y>(this IEnumerable<X> xs, Func<X, Y> selector) => xs.Map(selector);

		public static IEnumerable<Y> Lift2<TSource1, TSource2, Y>(IEnumerable<TSource1> source1,
			IEnumerable<TSource2> source2, Func<TSource1, TSource2, Y> func)
		{
			Contract.Requires(source1.Count() == source2.Count());
			return from tuple in source1.Zip(source2, (e1, e2) => new {First = e1, Second = e2})
				   select func(tuple.First, tuple.Second);
		}

		public static IEnumerable<Y> Lift3<TSource1, TSource2, TSource3, Y>(IEnumerable<TSource1> source1,
			IEnumerable<TSource2> source2, IEnumerable<TSource3> source3, Func<TSource1, TSource2, TSource3, Y> func)
		{
			Contract.Requires(source1.Count() == source2.Count());
			return from tuple in EmLinq.Zip3(source1, source2, source3, (e1, e2, e3) => new { First = e1, Second = e2, Third = e3 })
				   select func(tuple.First, tuple.Second, tuple.Third);
		}
	}
}
