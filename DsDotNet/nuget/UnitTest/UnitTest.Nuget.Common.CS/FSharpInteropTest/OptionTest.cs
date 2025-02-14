using Microsoft.FSharp.Core;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;
using System.Diagnostics;

namespace T
{
    [TestFixture]
    public class OptionTest
    {
        [Test]
        public void DoTest()
        {
            var s = new FSharpOption<int>(3);
            Assert.That(s.IsSome());

            var n = FSharpOption<int>.None;
            Assert.That(n.IsNone());

            var z = s.MatchMap(
                x => "OK",
                () => "NG"
                );
            Assert.That(z, Is.EqualTo("OK"));


            var z2 = n.MatchMap(
                x => "OK",
                () => "NG"
                );
            Assert.That(z2, Is.EqualTo("NG"));

            Assert.That(s.Map(n => n + 1).Value, Is.EqualTo(4));

            n.Match(
                x => Trace.WriteLine("What's up?"),
                () => Trace.WriteLine("OK, it's none!")
                );
        }
    }
}
