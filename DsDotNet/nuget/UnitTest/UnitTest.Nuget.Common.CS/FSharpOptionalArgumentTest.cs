using NUnit.Framework;
using NUnit.Framework.Internal;
using Dual.Common.Core.FS.Test;

namespace UnitTest.Nuget.Common.CS
{
    [TestFixture]
    public class FSharpOptionalArgumentTest
    {
        [Test]
        public void OptionalArgumentTest()
        {
            Assert.That(1.AddWithFSharpDefault(1), Is.EqualTo(2));
            // Assert.That(1.AddWithFSharpDefault(), Is.EqualTo(2)); --> Compile error

            Assert.That(1.AddWithDotNetDefault(), Is.EqualTo(2));
            Assert.That(1.AddWithDotNetDefault(3), Is.EqualTo(4));
        }
    }
}
