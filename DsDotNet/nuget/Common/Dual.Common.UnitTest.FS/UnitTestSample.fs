namespace Dual.Common.UnitTest.FS.Samples

open NUnit.Framework
open Dual.Common.UnitTest.FS
open Dual.Common.Core
open Newtonsoft.Json

#if SHOWSAMPLE
module FunctionStyle =
    [<SetUp>]
    let Setup () =
        ()

    [<Test>]
    let Test1 () =
        Assert.Pass()

module ClassStyle =
    type SampleTests() =
        inherit TestBaseClass(null)     // "mySampleLogger"

        [<Test>]
        member __.``assert test`` () =
            1 + 1 === 2

#endif