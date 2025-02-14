using log4net;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
	public static class EmTask
	{
		/// <summary> pp.20.  Concurrency in C# Cookbook </summary>
		public static Task<T> FromResult<T>(this T value) => Task.FromResult(value);
		public static Task<T> TaskYield<T>(this T value) => Task.FromResult(value);
		public static Task<T> TaskReturn<T>(this T value) => Task.FromResult(value);

		/// <summary>
		/// Map / Select
		/// </summary>
		// (a -> b) -> E<a> -> E<b>
		public static Task<TResult> TaskMap<TSource, TResult>(this Task<TSource> source, Func<TSource, TResult> func)
		{
			return Task.Run(async () =>
			{
				return func(await source);
			});
		}
		public static Task<TResult> Map<TSource, TResult>(this Task<TSource> source, Func<TSource, TResult> func) => TaskMap(source, func);


		/// <summary>
		/// Bind / FlatMap / Collect(F#) / SelectMany / Lift1
		/// </summary>
		// (a -> E<b>) -> E<a> -> E<b>
		public static Task<TResult> TaskBind<TSource, TResult>(this Task<TSource> source, Func<TSource, Task<TResult>> func)
		{
			return Task.Run(async () =>
			{
				return await func(await source);
			});
		}
		public static Task<TResult> Bind<TSource, TResult>(this Task<TSource> source, Func<TSource, Task<TResult>> func) => TaskBind(source, func);

		// return type : Task<IEnumerable<T>> ???
		public static Task<T[]> TaskSequence<T>(this IEnumerable<Task<T>> tasks)
		{
			return Task.WhenAll(tasks);
		}

		public static Task<T[]> Sequence<T>(this IEnumerable<Task<T>> tasks) => TaskSequence(tasks);

        public static bool ExecuteWithTimeLimit(this Action timeTakingAction, TimeSpan budget, Action onTimeOut=null)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(budget);
            var task = Task.Run(() => timeTakingAction(), cts.Token);
            if (! task.Wait(budget + TimeSpan.FromMilliseconds(1)) )
            {
				if (onTimeOut != null)
					onTimeOut();

				return false;
            }

			return task.IsCompleted;
        }

        // https://stackoverflow.com/questions/22629951/suppressing-warning-cs4014-because-this-call-is-not-awaited-execution-of-the
        public static void FireAndForget(this Task task, ILog logger = null)
        {
            task.ContinueWith(
                t => { logger?.Error(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}
