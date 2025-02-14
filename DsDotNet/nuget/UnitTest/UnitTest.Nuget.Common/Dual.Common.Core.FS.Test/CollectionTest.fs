namespace T

open Dual.Common.Core.FS
open NUnit.Framework
open System
open Newtonsoft.Json
open Dual.Common.UnitTest.FS
open Dual.Common.Base.FS
open Dual.Common.Core.FS
open Dual.Common.Base.CS

[<AutoOpen>]
module CollectionTest =

    [<Flags>]
    type SampleNumFlags =
        | BOOL  = 0x00000001
        | BYTE  = 0x00000002
        | WORD  = 0x00000004
        | DWORD = 0x00000008
    type SampleNumDU = | One | Two | Three

    [<TestFixture>]
    type AdhocPolymorphismTest() =
        [<Test>]
        member _.ContainsTest() =
            (Some 1 |> contains 1) === true
            (Some 1 |> contains 2) === false

            (Ok 1 |> contains 1) === true
            (Ok 1 |> contains 2) === false
            (Error 1 |> contains 1) === false

            [1; 2; 3;] |> contains 2 === true
            [1; 2; 3;] |> contains 5 === false
            "hello".ToCharArray() |> contains 'e' === true
            "hello" |> contains 'e' === true
            "hello" |> contains 'x' === false
            ["hello"; "world"] |> contains "hello" === true
            ["hello"; "world"] |> contains "HELLO" === false


            match [1; 2; 3;] with
            | Contains 1 -> ()
            | _ -> Assert.Fail()


        //[<Test>]
        //member _.PickTest() =
        //    let picker i x = if i = 1 then Some x else None
        //    let xxx = [1; 2; 3;] |> picki picker
        //    let yyy = [1; 2; 3;] |> Seq.picki picker
        //    [1; 2; 3;] |> picki picker === true
        //    //[1; 2; 3;] |> picki 2 === true

        [<Test>]
        member _.InterpolationTest() =
            let u = 20us
            $"%A{u}" === sprintf "%A" u

        [<Test>]
        member _.OptionTest() =
            let hello:string = Option.toReference (Some "hello")
            hello === "hello"
            SetSkipDuplicateLogWindowSize(3)
            DcLogger.EnableTrace <- true


            logDebug "hello"    // print
            logDebug "hello"    // no print
            logDebug "hello"
            logDebug "hello"

            logDebug "hello1"   // print
            logDebug "hello2"   // print
            logDebug "hello3"   // print
            logDebug "hello3"
            logDebug "hello2"
            logDebug "hello1"


            (Some "hello").ToReference() === "hello"

            let nullObj:obj = Option.toReference None
            nullObj === null
            (None).ToReference() === null

            // 컴파일 에러..
            // Option.toReference (Some 1) === 1
            // Option.toObj (Some 1) === 1
            // Option.toNullable (Some "hello") === "hello"

            let one:Nullable<int> = Option.toNullable (Some 1)
            one === 1

        [<Test>]
        member _.FSharpOptionInteropTest() =
            let f1 = fun value -> true
            let f2 = fun () -> false
            (Some 1).Match(
                            (fun value -> true),
                            (fun () -> failwith "ERROR") ) === true

            (None).Match(
                            (fun value -> failwith "ERROR"),
                            (fun () -> true) ) === true


        [<Test>]
        member _.DynamicDictionaryTest() =
            let dic =
                let kvs:seq<string*obj> =
                    [
                        ("one", 1)
                        ("two", 2)
                        ("three", 3)
                        ("pi", 3.14)
                        ("list", [1; 2; 3;])
                        ("array", [|1; 2; 3;|])
                        ("date-min", DateTime.MinValue)
                    ]
                kvs |> DynamicDictionary

            dic.TryGetInt("one") === Some 1
            dic.TryGetInt("hundred") === None
            dic.TryGetDouble("pi") === Some 3.14
            dic.TryGet<DateTime>("date-min") === Some DateTime.MinValue
            dic.TryGet<List<int>>("list") === Some [1; 2; 3;]
            dic.TryGet<int array>("array") === Some [|1; 2; 3;|]


            dic.GetInt("one") === 1
            (fun () -> dic.GetInt("hundred") |> ignore) |> ShouldFail
            dic.GetDouble("pi") === 3.14
            dic.Get<DateTime>("date-min") === DateTime.MinValue
            dic.Get<List<int>>("list") === [1; 2; 3;]
            dic.Get<int array>("array") === [|1; 2; 3;|]
        [<Test>]
        member _.``iteri test``() =
            [1..10] |> iteri (fun i x -> tracefn "[%d] : %d" i x)


        [<Test>]
        member _.``operator !! test``() =
            let boolFunction (b:bool) = b
            not true === !! true
            not <| boolFunction(true) === !! boolFunction(true)

            // 불가능 : not boolFunction(true)
            // 불가능 : !! boolFunction true

        [<Test>]
        member _.``null test``() =
            let nullStr:string = null
            "someStr" |> Null.defaultValue "hello" === "someStr"
            "someStr" |> Null.defaultWith (fun () -> "hello") === "someStr"

            nullStr |> Null.defaultValue "hello" === "hello"
            nullStr |> Null.defaultWith (fun () -> "hello") === "hello"


        [<Test>]
        member _.``DU/enum test``() =
            let duCases = DU.Cases<SampleNumDU>()
            let flagCases = DU.Cases<SampleNumFlags>()
            duCases |> map toString === ["One"; "Two"; "Three"]
            flagCases |> map toString === ["BOOL"; "BYTE"; "WORD"; "DWORD"]
            SeqEq duCases [SampleNumDU.One; SampleNumDU.Two; SampleNumDU.Three]
            SeqEq (flagCases |> map (fun x -> x :?> SampleNumFlags)) [SampleNumFlags.BOOL; SampleNumFlags.BYTE; SampleNumFlags.WORD; SampleNumFlags.DWORD]


        [<Test>]
        member _.``collection to option test``() =
            [].SeqToOption() === None
            [||].SeqToOption() === None
            Seq.empty.SeqToOption() === None

            [0..1].SeqToOption().Value === [0..1]
            [|0..1|].SeqToOption().Value  === [|0..1|]
            (seq {0..1}).SeqToOption().Value |> SeqEq (seq {0..1})

            (* Net9.0 부터 지원되지 않는 형식
            [].TryMinBy(id) === None
            [0..10].TryMinBy(id) === Some 0
            *)
