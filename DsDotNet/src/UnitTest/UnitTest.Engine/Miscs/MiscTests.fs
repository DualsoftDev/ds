namespace T

open NUnit.Framework
open Engine.Core
open Engine.Common.FS.Log4NetWrapper
open Engine.Common.FS


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
