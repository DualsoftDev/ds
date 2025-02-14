namespace T

open System.Text.Json
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module rec ``I'm recursive module`` =
    [<TestFixture>]
    type RecTest() =
        [<Test>]
        member x.RecTest() =
            let ns = ["zero"; "one"; "two"; "three"; "four"; "five"]
            fun3() === 3
            (3).StaticExtensionMethodIncr() === 4
            x.FSharpTypeExtension() === "F#"

    let fun3() = 3

    type ``I'm extension type`` =
        /// type 내의 [<Extension>] method
        [<Extension>] static member StaticExtensionMethodIncr(x:int) = x + 1

    type RecTest with
        member x.FSharpTypeExtension() = "F#"