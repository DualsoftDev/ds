namespace T

open NUnit.Framework


[<AutoOpen>]
module MiscTestModule =
    type MiscTests() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``NewtonSoft version test`` () =
            let get_newtonsoft_json_version() =
                let assembly = typeof<Newtonsoft.Json.JsonConvert>.Assembly
                let version = assembly.GetName().Version
                version

            let ver = get_newtonsoft_json_version()
            let major, minor = ver.Major, ver.Minor
            major >= 13 === true
