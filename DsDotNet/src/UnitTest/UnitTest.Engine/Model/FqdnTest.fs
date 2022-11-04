namespace UnitTest.Engine


open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open System.Linq
open Antlr4.Runtime

[<AutoOpen>]
module FqdnTests =
    type FqdnTest() =
        do Fixtures.SetUpTest()
        let seqEq(a, b) = Enumerable.SequenceEqual(a, b) |> ShouldBeTrue
        let (=?=) x y = seqEq(x, y)
        [<Test>]
        member __.``Fqdn test`` () =
            Fqdn.parse "a" =?= [ "a" ]
            (fun () -> Fqdn.parse "3" |> ignore )
                |> ShouldFailWithSubstringT("Lexical")  //  <LexerNoViableAltException> //"LexerNoViableAltException"

            Fqdn.parse "a.b.c" =?= [ "a"; "b"; "c" ]
            Fqdn.parse """"3.14 = PI".a.b.c""" =?= [ "\"3.14 = PI\""; "a"; "b"; "c" ]
