namespace T
open Dual.UnitTest.Common.FS

open NUnit.Framework
open Engine.Core
open Dual.Common.Core.FS.Log4NetWrapper
open Dual.Common.Core.FS


[<AutoOpen>]
module MiscTestModule =
    type MiscTests() =
        inherit EngineTestBaseClass()

        [<Test>]
        member __.``NewtonSoft version test`` () =
            let get_newtonsoft_json_version() =
                let assembly = typeof<Newtonsoft.Json.JsonConvert>.Assembly
                let version = assembly.GetName().Version
                version

            let ver = get_newtonsoft_json_version()
            let major, minor = ver.Major, ver.Minor
            major >= 13 === true


        [<Test>]
        member __.``Unique name generator test`` () =
            [ for i in 1..5 -> UniqueName.generate "MyTON" ]
            |> SeqEq ["MyTON0"; "MyTON1"; "MyTON2"; "MyTON3"; "MyTON4"; ]

            [ for i in 1..5 -> UniqueName.generate "MyCTU" ]
            |> SeqEq ["MyCTU0"; "MyCTU1"; "MyCTU2"; "MyCTU3"; "MyCTU4"; ]

            [ for i in 1..5 -> UniqueName.generate "MyTON" ]
            |> SeqEq ["MyTON5"; "MyTON6"; "MyTON7"; "MyTON8"; "MyTON9"; ]

            [ for i in 1..5 -> UniqueName.generate "MyCTU" ]
            |> SeqEq ["MyCTU5"; "MyCTU6"; "MyCTU7"; "MyCTU8"; "MyCTU9"; ]

            UniqueName.resetAll()

            [ for i in 1..5 -> UniqueName.generate "MyTON" ]
            |> SeqEq ["MyTON0"; "MyTON1"; "MyTON2"; "MyTON3"; "MyTON4"; ]

        [<Test>]
        member __.``Unique name generator case insensitive test`` () =
            let a0 = UniqueName.generate "A"
            let a1 = UniqueName.generate "a"
            let a2 = UniqueName.generate "A"
            a0 === "A0"
            a1 === "a1"
            a2 === "A2"

            UniqueName.resetAll()
            let a0 = UniqueName.generate "A"
            let a1 = UniqueName.generate "a"
            let a2 = UniqueName.generate "A"
            a0 === "A0"
            a1 === "a1"
            a2 === "A2"

            ()
        [<Test>]
        member __.``Fail test`` () =
            tracefn "FAIL testing..."
            try
                failwithstack "Dying with stack trace!"
            with _ ->
                ()

        [<Test>]
        member __.``String option test`` () =
            let nullstr:string = null
            let emptystr = ""
            let hello = "hello"
            let optnullstr = nullstr |> Option.ofString
            nullstr  |> Option.ofString |> Option.isNone === true
            emptystr |> Option.ofString |> Option.isNone === true
            hello    |> Option.ofString |> Option.isSome === true

            nullstr  |> String.orElse "world" === "world"
            emptystr |> String.orElse "world" === "world"
            hello    |> String.orElse "world" === "hello"

        [<Test>]
        member __.``OrElse test`` () =
            orElse [3] [1] === [1]
            orElse [3] []  === [3]
            orElse [] []   === []
            orElse (Some 3) None === Some 3
            orElse (Some 3) (Some 5) === Some 5
            sprintf "%A" <| orElse (Ok 3) (Error "Error") === sprintf "%A" (Ok 3)
            //orElse (Ok 3) (Error "Error") === (Ok 3)
            orElse (Ok 3) (Ok 5) === Ok 5


            [Some 1; None; Some 3] |> List.mapSome ((+) 1) === [2; 4]


        [<Test>]
        member __.``Generic test`` () =
            let sys = DsSystem.Create4Test("testSys")
            //RuntimeDS.Target <- XGI
            RuntimeDS.System <- Some sys

            let anal(v:IValue<'T>) =
                let innerType = typedefof<'T>

                let varType     = typedefof<Variable<_>>     .GetGenericTypeDefinition().MakeGenericType(innerType)
                let tagType     = typedefof<Tag<_>>          .GetGenericTypeDefinition().MakeGenericType(innerType)
                let literalType = typedefof<LiteralHolder<_>>.GetGenericTypeDefinition().MakeGenericType(innerType)
                let vType = v.GetType()
                if vType = varType then
                    $"Variable<{innerType.Name}>"
                elif vType = tagType then
                    $"Tag<{innerType.Name}>"
                elif vType = literalType then
                    $"LiteralHolder<{innerType.Name}>"
                else
                    $"Something Else: {vType.Name}"

            let param = defaultStorageCreationParams(false) (VariableTag.PcUserVariable|>int)
            let v = new Variable<bool> {param with Name="test var"; }
            anal(v) === "Variable<Boolean>"

            let t = new Tag<bool> {param with Name="test var"; }
            anal(t) === "Tag<Boolean>"

            let l = {Value=1}:LiteralHolder<int>
            anal(l) === "LiteralHolder<Int32>"

            let tvs = t :> TypedValueStorage<bool>
            let ivb = t :> IValue<bool>
            anal(tvs) === "Tag<Boolean>"
            anal(ivb) === "Tag<Boolean>"

            match v.GetType() with
            | GenericType <@ typedefof<Variable<_>> @> [|t|] -> $"Variable<{t.Name}>"//addChildListUntyped(t,o)
            | _ -> failwithlog "ERROR"
            === "Variable<Boolean>"

            (* IValue<'T> 를 TypedValueStorage<obj> 가 아닌, TypedValueStorage<'T> 로 변환 할 수 있나? *)
            //let terminal = DuVariable (ivb :?> TypedValueStorage<_>)
            ()