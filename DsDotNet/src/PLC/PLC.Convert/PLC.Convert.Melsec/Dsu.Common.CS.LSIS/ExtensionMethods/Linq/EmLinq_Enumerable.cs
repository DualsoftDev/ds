using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

// Understanding map and apply
// http://fsharpforfunandprofit.com/posts/elevated-world/

namespace Dsu.Common.CS.LSIS.ExtensionMethods
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
		/// Generic 이 아닌 IEnumerable 을 Generic 으로 변환
		public static IEnumerable<T> ToEnumerable<T>(this IEnumerable source)
		{
			foreach (var item in source)
				yield return (T)item;
		}

		public static IEnumerable<T> Return<T>(this T item) => item.ToEnumerable();
		public static IEnumerable<T> Yield<T>(this T item) => item.ToEnumerable();
		public static IEnumerable<T> ReturnEnumerable<T>(this T item) => item.ToEnumerable();
        public static IEnumerable<T> IfNullOrEmpty<T>(this IEnumerable<T> first, IEnumerable<T> second) => first.IsNullOrEmpty() ? second : first;
	    public static IEnumerable<T> CreateEmptySequence<T>() => Enumerable.Empty<T>();



        public static IEnumerable<TResult> Map<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source.Select(selector);
		public static IEnumerable<TResult> MapEnumerable<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source.Select(selector);
		public static IEnumerable<TResult> MapEnumerable<TSource, TResult>(this Func<TSource, TResult> selector, IEnumerable<TSource> source) => source.Select(selector);


		/// <summary>
		/// Reduce / Fold / Aggregate
		/// </summary>
		public static TSource Reduce<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func) => source.Aggregate(func);
		public static TAccumulate Reduce<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func) => source.Aggregate(seed, func);
		public static TResult Reduce<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector) => source.Aggregate(seed, func, resultSelector);
		public static TSource Fold<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func) => source.Aggregate(func);
		public static TAccumulate Fold<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func) => source.Aggregate(seed, func);
		public static TResult Fold<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector) => source.Aggregate(seed, func, resultSelector);


		/// <summary>
		/// Filter / Where
		/// </summary>
		public static IEnumerable<TSource> Filter<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) => source.Where(predicate);

		/// <summary>
		/// Bind / FlatMap / Collect(F#) / SelectMany / Lift1
		/// </summary>
		public static IEnumerable<TResult> Bind<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector) => source.SelectMany(selector);
		public static IEnumerable<TResult> Bind<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector) => source.SelectMany(selector);
		public static IEnumerable<TResult> Bind<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector) => source.SelectMany(collectionSelector, resultSelector);
		public static IEnumerable<TResult> Bind<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector) => source.SelectMany(collectionSelector, resultSelector);


		public static IEnumerable<TResult> Apply<TSource, TResult>(this IEnumerable<Func<TSource, TResult>> funcs, IEnumerable<TSource> source)
		{
			return from f in funcs
				from s in source
				select f(s);
		}


		public static IEnumerable<TResult> Lift1<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source.Map(selector);

		public static IEnumerable<TResult> Lift2<TSource1, TSource2, TResult>(IEnumerable<TSource1> source1,
			IEnumerable<TSource2> source2, Func<TSource1, TSource2, TResult> func)
		{
			Contract.Requires(source1.Count() == source2.Count());
			return from tuple in source1.Zip(source2, (e1, e2) => new {First = e1, Second = e2})
				   select func(tuple.First, tuple.Second);
		}
	}
}
