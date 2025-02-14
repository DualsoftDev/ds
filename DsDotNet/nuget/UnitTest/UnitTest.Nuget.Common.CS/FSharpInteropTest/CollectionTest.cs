using System;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;
using System.Linq;

namespace T
{
    [TestFixture]
    public class CollectionTest
    {
        private static readonly int[] expected = new[] { 10 };

        [Test]
        public void DoTest()
        {
            var answer = new[] { Tuple.Create(1, 3), Tuple.Create(1, 4), Tuple.Create(2, 3), Tuple.Create(2, 4) };
            Assert.That(new[] { 1, 2 }.AllPairs([3, 4]), Is.EqualTo(answer));

            Assert.That(
                Enumerable.Range(1, 10).ToArray().ChunkBySize(3),
                Is.EqualTo(
                    new[] {
                        [1, 2, 3],
                        [4, 5, 6],
                        [7, 8, 9],
expected, }));
            Console.WriteLine();
        }

        [Test]
        public void NthTest()
        {
            // Test for Seq.nth equivalent
            var sequence = Enumerable.Range(1, 10).ToArray().AsEnumerable();
            Assert.That(sequence.Nth(4), Is.EqualTo(5));

            // Test for Array.nth equivalent
            var array = new[] { "a", "b", "c", "d", "e" };
            Assert.That(array.Nth(2), Is.EqualTo("c"));
        }
    }
}
