using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;

namespace T
{
    [TestFixture]
    public class ResultTest
    {
        [Test]
        public void DoTest()
        {
            var ok =  FSharpResult<double, string>.NewOk(3.14);
            Assert.That(ok.IsOk);
            Assert.That(ok.DefaultValue(0), Is.EqualTo(3.14));

            var ok2 = ok.Map(v => v * 2);
            Assert.That(ok2.IsOk);
            Assert.That(ok2.ResultValue, Is.EqualTo(6.28));


            var err =  FSharpResult<double, string>.NewError("Fail");
            Assert.That(err.IsError);
            Assert.That(err.ErrorValue, Is.EqualTo("Fail"));
            Assert.That(err.DefaultValue(0), Is.EqualTo(0));
            Assert.That(err.DefaultWith(err => 3.0), Is.EqualTo(3.0));

            Assert.That(err.Map(v => v * 2).IsError);



            Console.WriteLine("");
        }
    }
}
