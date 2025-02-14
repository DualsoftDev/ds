namespace T

open System.Text.Json
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS

[<AutoOpen>]
module DisposableBuilderTestModule =


    [<TestFixture>]
    type DisposableBuilderTest() =
        [<Test>]
        member _.``DisposableBuilder basics``() =
            let mutable state = 2
            let disposable = disposable { state <- 1 }
            disposable.Dispose()
            state === 1
