using System;

using NUnit.Framework;
using NUnit.Framework.Internal;

namespace UnitTest.Nuget.Common.CS
{
    [TestFixture]
    public class CsCoreTest
    {
        Func<int, Func<int, int>> add = x => y => x + y;

        [Test]
        public void CSharpCurryTest()
        {
            Assert.That(true, "What the hell?");
            var add2 = add(2);
            var z = add2 (3);
            Assert.That(z, Is.EqualTo(5));

            Assert.That(add(3)(8), Is.EqualTo(11));
        }
    }
}
