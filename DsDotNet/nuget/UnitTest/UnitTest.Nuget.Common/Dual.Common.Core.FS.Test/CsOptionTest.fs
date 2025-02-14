namespace T

open System
open System.Text.Json
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS

[<AutoOpen>]
module CsOptionTest =

    [<AllowNullLiteral>]
    type RefClassSample() =
        member val Value = 1 with get, set
        member val Name = "" with get, set

    [<TestFixture>]
    type CSharpOptionTest() =
        (* F# Option 을 직접 사용할 것. *)

        //[<Test>]
        //member _.CsOptionReferenceTest() =
        //    let one = CsOption.Some "ONE"
        //    one.HasValue === true

        //    logInfo "Hello"

        //    RefClassSample() |> CsOption.Some |> fun x -> x.IsSome === true

        //[<Test>]
        //member _.CsOptionTest() =
        //    let one = CsOption.Some 1
        //    let none = CsOption.None ()
        //    one.HasValue === true
        //    none.HasValue === false

        //    one.Map(fun x -> x + 1).Value === 2
        //    one.Bind(fun x -> CsOption.Some(x + 1)).Value === 2
        //    none.Map(fun x -> x + 1) === none
        //    none.Bind(fun x -> CsOption.Some(x + 1)) === none

        //    let options = [ // CsOption<int> list
        //        CsOption.Some(2)
        //        CsOption.None ()
        //        CsOption<int>.Some(5)
        //        CsOption<int>.Some(3)
        //    ]

        //    options.CsChoose(fun x -> x) |> Seq.toList === [2; 5; 3]

        [<Test>]
        member _.CsOptionSerializeTest() =
            let sample1 = RefClassSample(Name="test", Value=2) |> OptionSerializable.Some
            let json = JsonSerializer.Serialize(sample1)
            let sample2 = JsonSerializer.Deserialize<OptionSerializable<RefClassSample>>(json)
            let json2 = JsonSerializer.Serialize(sample2)
            json2 === json


            let none = OptionSerializable<RefClassSample>.None()
            let jsonNone = JsonSerializer.Serialize(none)
            let none2 = JsonSerializer.Deserialize<OptionSerializable<RefClassSample>>(jsonNone)
            let jsonNone2 = JsonSerializer.Serialize(none2)
            jsonNone2 === jsonNone

            let ref = RefClassSample()
            let opt = OptionSerializable<RefClassSample>.FromReference(ref)
            opt.IsSome === true
            OptionSerializable<RefClassSample>.FromReference(null).IsNone === true



        [<Test>]
        member _.OptionConversionTest() =
            Nullable<int>(3).ToOption() === Some 3
            Nullable<int>(3) |> Option.ofNullable === Some 3
            Nullable<int64>(3L).ToOption() === Some 3L
            Nullable<int64>(3L) |> Option.ofNullable === Some 3L

