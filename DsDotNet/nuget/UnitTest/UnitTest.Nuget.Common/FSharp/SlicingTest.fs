namespace T

open System.Text.Json
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS

[<AutoOpen>]
module SlicingTestModule =
    [<TestFixture>]
    type SlicingTest() =
        [<Test>]
        member _.SlicingTest() =
            let ns = ["zero"; "one"; "two"; "three"; "four"; "five"]
            ns[0..1] === ["zero"; "one"]
            ns[1..2] === ["one"; "two"]
            ns[3..5] === ["three"; "four"; "five"]
            ns[..2] === ["zero"; "one"; "two"]
            ns[3..] === ["three"; "four"; "five"]

            (* 음의 index 사용하면 empty 반환 *)
            ns[-2..-1] |> SeqEq []

            let l = [ 1..10 ]
            let a = [| 1..10 |]
            let s = "hello!"

            // Before: would return empty list
            // F# 5: same
            let emptyList = l[-2..(-1)]
            emptyList === ([]:int list)

            // Before: would throw exception
            // F# 5: returns empty array
            let emptyArray = a[-2..(-1)]
            emptyArray === ([||]:int [])

            // Before: would throw exception
            // F# 5: returns empty string
            let emptyString = s[-2..(-1)]
            emptyString === ""

