namespace T

open System
open System.Text.Json
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS

[<AutoOpen>]
module ChooseSeqTestModule =


    [<TestFixture>]
    type ChooseSeqTestTest() =
        [<Test>]
        member _.``ChooseSeq basics``() =
            let chosen =
                chooseSeq {
                    let! one = Some 1
                    yield one
                    let! none = None
                    // -- let! None  이후는 평가 안됨
                    yield none
                    let! two = Some 2
                    yield two
                } |> toArray
            chosen === [| 1 |]

            let chosen =
                chooseSeq {
                    for opt in [Some 1; None; Some 2; ] do      // for 내부에 존재하는 None 은 skip 하고 계속 진행 가능
                        let! v = opt
                        yield v
                } |> toArray
            chosen === [| 1; 2 |]




            (* Nullable with option test *)
            option {
                let! x = System.Nullable 42
                return x + 1
            } === Some 43

            let somePi =
                option {
                    return Nullable 3.14
                }
            somePi.IsSome === true
            somePi.Value === 3.14

            option {
                return! Nullable()
            } === None

