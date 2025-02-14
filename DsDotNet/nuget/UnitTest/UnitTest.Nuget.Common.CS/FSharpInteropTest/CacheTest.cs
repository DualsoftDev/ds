using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;
using System.Diagnostics;

namespace T
{
    [TestFixture]
    public class CacheTest
    {
        [Test]
        public void DoTest()
        {
            int totalNumGenerated = 0;
            IEnumerable<int> generate()
            {
                for (int i = 0; i < 10; i++)
                {
                    Trace.WriteLine($"Generating {i}");
                    totalNumGenerated++;
                    yield return i;
                }
            }
            var cachedXs = generate().Cache();

            // Cache allows multiple iterations without recomputation
            Assert.That(cachedXs.Nth(2), Is.EqualTo(2));
            Assert.That(totalNumGenerated, Is.EqualTo(3));      // 0, 1, 2
            Assert.That(cachedXs.Nth(1), Is.EqualTo(1));
            Assert.That(totalNumGenerated, Is.EqualTo(3));      // No more generation


            // 초기화 및 remove cache
            totalNumGenerated = 0;
            var cacheKiller = cachedXs as IDisposable;
            cacheKiller.Dispose();
            Assert.That(cachedXs.Nth(2), Is.EqualTo(2));
            Assert.That(totalNumGenerated, Is.EqualTo(3));      // 0, 1, 2
            Assert.That(cachedXs.Nth(1), Is.EqualTo(1));
            Assert.That(totalNumGenerated, Is.EqualTo(3));      // No more generation

            var arr = cachedXs.ToArray();
            Assert.That(totalNumGenerated, Is.EqualTo(10));



        }


    }
}
