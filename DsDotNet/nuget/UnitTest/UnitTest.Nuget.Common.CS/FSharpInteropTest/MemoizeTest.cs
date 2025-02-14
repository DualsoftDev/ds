using System;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;

namespace T
{
    [TestFixture]
    public class MemoizeTest
    {
        [Test]
        public void DoTest()
        {
            //int callCount = 0;
            //Func<int, int> slowFunction = x =>
            //{
            //    callCount++;
            //    return x * x;
            //};

            //var memoizedFunction = slowFunction.Memoize();

            int callCount = 0;
            var memoizedFunction = EmFunction.Memoize((int x) => { callCount++; return x * x; });
            //var firstResult = memoizedFunction.Invoke(5);
            //var secondResult = memoizedFunction.Invoke(5);

            int memoized(int x) => memoizedFunction.Invoke(x);

            // Assert
            Assert.That(callCount, Is.EqualTo(0));         // 함수는 한 번만 호출되어야 함
            Assert.That(memoized(5), Is.EqualTo(25));       // 첫 번째 호출의 결과는 25이어야 함
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(memoized(5), Is.EqualTo(25));      // 두 번째 호출의 결과도 25이어야 함
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(memoized(6), Is.EqualTo(36));
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(memoized(6), Is.EqualTo(36));
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(memoized(5), Is.EqualTo(25));
            Assert.That(callCount, Is.EqualTo(2));

            var xs = EmFunction.GetCallStackFunctionNames();
            var x = EmFunction.GetFunctionName();
            Console.WriteLine();

        }

    }
}
