using System;
using System.Threading.Tasks;

namespace Dsu.Common.CS.LSIS.ExtensionMethods
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
		/// 실제로 이런 기능이 쓰이는가?
		/// </summary>
		public static async Task<U> Apply<T, U>(this Task<Func<T, U>> funcs, Task<T> source)
		{
			var f = await funcs;
			return f(await source);
		}
	}
}
