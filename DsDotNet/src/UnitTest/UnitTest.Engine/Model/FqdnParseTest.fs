namespace UnitTest.Engine

open System
open Engine.Core
open NUnit.Framework
open Engine.Cpu.Expression
open Engine.Parser.FS
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser

[<AutoOpen>]
module FqdnParseTestModule =
    let toString x = x.ToString()
    type FqdnParseTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``Fqdn parse test`` () =
            parseFqdn("hello") === ["hello"]
            parseFqdn("A.B.C") === [ "A"; "B"; "C" ]
            parseFqdn("#seg.testMe!!!") === ["#seg.testMe!!!"]

        [<Test>]
        member __.``Combine test`` () =
            ["#Seg.Complex#"].Combine() === "#Seg.Complex#"
