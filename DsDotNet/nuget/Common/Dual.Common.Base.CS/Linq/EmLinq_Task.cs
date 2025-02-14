using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dual.Common.Base.CS
{
	// see TaskExtensions on LanguageExt.Core.dll
	public static partial class EmLinq
	{
		/// <summary>
		/// Convert a value to a Task that completes immediately
		/// </summary>
		public static Task<T> ToTask<T>(this T item) => Task.FromResult(item);
		public static Task<T> ReturnTask<T>(this T item) => Task.FromResult(item);

		public static async Task<U> Map<T, U>(this Func<T, U> map, Task<T> self) => map(await self);
		public static async Task<U> MapTask<T, U>(this Task<T> self, Func<T, U> map) => map(await self);
		public static async Task<U> Bind<T, U>(this Task<T> self,Func<T, Task<U>> bind) => await bind(await self);
		public static async Task<U> BindTask<T, U>(this Func<T, Task<U>> bind, Task<T> self) => await bind(await self);
		public static async Task<U> SelectMany<T, U>(this Task<T> self, Func<T, Task<U>> bind) => await Bind(self, bind);

		/// <summary>
		/// 비동기 람다식을 지원하는 SelectManyAsync
		/// </summary>
        public static async Task<IEnumerable<TResult>> SelectManyAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<IEnumerable<TResult>>> selector)
        {
            var results = await Task.WhenAll(source.Select(selector));
            return results.SelectMany(x => x);
        }

        /// <summary>
        /// 비동기 람다식을 지원하는 SelectAsync
        /// </summary>
        public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> selector)
        {
            var tasks = source.Select(selector);
            return await Task.WhenAll(tasks);
        }

        public static async Task<U> Apply<T, U>(this Task<Func<T, U>> funcs, Task<T> source)
		{
			var f = await funcs;
			return f(await source);
		}
	}
}
