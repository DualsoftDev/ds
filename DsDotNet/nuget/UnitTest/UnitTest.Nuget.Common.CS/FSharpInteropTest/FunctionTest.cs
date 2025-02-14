using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;
using NUnit.Framework.Legacy;

namespace T
{
    [TestFixture]
    public class FunctionTest
    {
        [Test]
        public void DurationTest()
        {
            var ms = DcTimer.Duration(() => System.Threading.Thread.Sleep(100));
            Assert.That(ms, Is.InRange(100, 500));

            (var result, var ms2) = DcTimer.Duration(() => { System.Threading.Thread.Sleep(100); return "Nice"; });
            Assert.That(result, Is.EqualTo("Nice"));
            Assert.That(ms2, Is.InRange(100, 500));
        }

        [Test]
        public void ComposeTest()
        {
            Func<int, int> square = x => x * x;
            Func<int, int> increment = x => x + 1;

            Assert.That(square.Compose(increment)(4), Is.EqualTo(17));       // 4^2 + 1 = 17

            var composedFunc1 = square.Compose(increment);
            Assert.That(composedFunc1(4), Is.EqualTo(17));


            var composedFunc2 = EmFunction.Compose(square, increment);
            Assert.That(composedFunc2(4), Is.EqualTo(17));


            Assert.That(increment.ComposeNTimes(10)(1), Is.EqualTo(11));    // 1 을 10번 increment => 11
        }

        [Test]
        public void TeeTest()
        {

            int sideEffectValue = 0;
            Action<int> sideEffect = x => sideEffectValue = x;

            // Act
            var result = EmFunction.Tee(10, sideEffect);

            Assert.That(result, Is.EqualTo(10));    // 반환 값은 원래 값이어야 함
            Assert.That(sideEffectValue, Is.EqualTo(10));    // side-effect 가 적용되어야 함
        }

        [Test]
        public void TeeForEachTest()
        {
            IEnumerable<int> sequence = new List<int> { 1, 2, 3, 4, 5 };

            int sum = 0;
            var result = sequence.TeeForEach(x => sum += x);


            CollectionAssert.AreEqual(sequence, result, "반환 값은 원래 시퀀스와 동일해야 하는데.. 쩝..");
            Assert.That(sum, Is.EqualTo(15));    // 각 요소에 대한 side-effect가 적용되어야 함
        }


    }
}
