namespace T
open Dual.Common.UnitTest.FS

open Engine.Core
open NUnit.Framework
open Engine.Parser.FS

[<AutoOpen>]
module FqdnParseTestModule =
    let toString x = x.ToString()
    type FqdnParseTest() =
        inherit EngineTestBaseClass()
        let parseFqdn = FqdnParserModule.parseFqdn

        [<Test>]
        member __.``Fqdn parse test`` () =
            parseFqdn("hello") === [|"hello"|]
            parseFqdn("A.B.C") === [| "A"; "B"; "C" |]
            parseFqdn("#seg.testMe!!!") === [|"#seg.testMe!!!"|]

        [<Test>]
        member __.``Combine test`` () =
            ["#Seg.Complex#"].Combine() === "#Seg.Complex#"
