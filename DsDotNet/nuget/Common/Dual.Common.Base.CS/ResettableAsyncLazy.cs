using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dual.Common.Base.CS
{
    public class ResettableAsyncLazy<T> : ResettableLazy<Task<T>>
	{
		public ResettableAsyncLazy(
				Func<Task<T>> valueFactory,
				LazyThreadSafetyMode lazyThreadSafetyMode = LazyThreadSafetyMode.ExecutionAndPublication
			)
			: base(valueFactory, lazyThreadSafetyMode)
		{
		}

		public Task<T> GetValueAsync() => Value;
	}

	/*
Console.WriteLine($"{DateTime.Now}: Start program");
ResettableLazy<Task<int>> myAsyncLazyMap = new ResettableLazy<Task<int>>(() => AsyncFunction());
async Task<int> AsyncFunction()
{
    // 임의의 비동기 작업 수행
    await Console.Out.WriteAsync($"Waiting...{DateTime.Now}");
    await Task.Delay(5000); // 예: 5초 지연
	await Console.Out.WriteLineAsync($"  Done. {DateTime.Now}");
	return 42; // 예: 계산된 값 반환
}

Console.WriteLine($"result={await myAsyncLazyMap.Value} on {DateTime.Now}");
Console.WriteLine($"result={await myAsyncLazyMap.Value} on {DateTime.Now}");
Console.WriteLine($"result={await myAsyncLazyMap.Value} on {DateTime.Now}");
Console.WriteLine($"result={await myAsyncLazyMap.Value} on {DateTime.Now}");

Console.WriteLine($"{DateTime.Now}: Start program");
ResettableAsyncLazy<int> myAsyncLazyMap2 = new (() => AsyncFunction());
Console.WriteLine($"result={await myAsyncLazyMap2.GetValueAsync()} on {DateTime.Now}");
Console.WriteLine($"result={await myAsyncLazyMap2.GetValueAsync()} on {DateTime.Now}");
Console.WriteLine($"result={await myAsyncLazyMap2.GetValueAsync()} on {DateTime.Now}");
Console.WriteLine($"result={await myAsyncLazyMap2.GetValueAsync()} on {DateTime.Now}");



2024-03-22 오후 1:55:21: Start program
Waiting...2024-03-22 오후 1:55:21  Done. 2024-03-22 오후 1:55:26
result=42 on 2024-03-22 오후 1:55:26
result=42 on 2024-03-22 오후 1:55:26
result=42 on 2024-03-22 오후 1:55:26
result=42 on 2024-03-22 오후 1:55:26
2024-03-22 오후 1:55:26: Start program
Waiting...2024-03-22 오후 1:55:26  Done. 2024-03-22 오후 1:55:31
result=42 on 2024-03-22 오후 1:55:31
result=42 on 2024-03-22 오후 1:55:31
result=42 on 2024-03-22 오후 1:55:31
result=42 on 2024-03-22 오후 1:55:31

	 */
}
