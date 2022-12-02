namespace UnitTest.Engine

open Engine.Core
open NUnit.Framework
open Engine.Parser.FS

[<AutoOpen>]
module FqdnParseTestModule =
    let toString x = x.ToString()
    type FqdnParseTest() =
        do Fixtures.SetUpTest()
        let parseFqdn = FqdnParser.parseFqdn

        [<Test>]
        member __.``Fqdn parse test`` () =
            parseFqdn("hello") === ["hello"]
            parseFqdn("A.B.C") === [ "A"; "B"; "C" ]
            parseFqdn("#seg.testMe!!!") === ["#seg.testMe!!!"]

        [<Test>]
        member __.``Combine test`` () =
            ["#Seg.Complex#"].Combine() === "#Seg.Complex#"
