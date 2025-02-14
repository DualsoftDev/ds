namespace T

open System.Text.Json
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.PLC.TagParser.FS

[<AutoOpen>]
module LsXgkTagParserTestModule =

    [<TestFixture>]
    type LsXgkTagParserTest() =
        let WORD, BIT = 16, 1
        let A, B, C, D, E, F = 10, 11, 12, 13, 14, 15
        let FILE = 32

        [<Test>]
        member _.``Test Standard``() =

            tryParseXgkTag "P00000" === Some("P", BIT, 0)
            tryParseXgkTag "P00001" === Some("P", BIT, 1)
            tryParseXgkTag "P0000A" === Some("P", BIT, A)
            LsXgkTagParser.Parse "P00000" === ("P", BIT, 0)

            tryParseXgkTag "P00010" === Some("P", 1, WORD + 0)
            tryParseXgkTag "P00011" === Some("P", 1, WORD + 1)
            tryParseXgkTag "P0001A" === Some("P", 1, WORD + 10)

            tryParseXgkTag "M00000" === Some("M", BIT, 0)
            tryParseXgkTag "F00000" === Some("F", BIT, 0)
            tryParseXgkTag "K00000" === Some("K", BIT, 0)


            tryParseXgkTag "P0000" === Some("P", WORD, 0)
            tryParseXgkTag "P0001" === Some("P", WORD, 16)
            tryParseXgkTag "P0002" === Some("P", WORD, 32)
            tryParseXgkTag "P000A" === None
            LsXgkTagParser.Parse "P000A" === null

            tryParseXgkTag "P0010" === Some("P", WORD, 16*10)
            tryParseXgkTag "P0032" === Some("P", WORD, 16*32)



            tryParseXgkTag "R3"   === Some("R", WORD, 16*3)
            tryParseXgkTag "R3.1" === Some("R", BIT,  16*3 + 1)


            tryParseXgkTag "U3.1"     === Some("U", WORD, (3 * FILE + 1) * 16)
            tryParseXgkTag "U3.1.A"   === Some("U", BIT,  (3 * FILE + 1) * 16 + A)
            tryParseXgkTag "U3.A"     === None

            tryParseXgkTag "S3.1"   === Some("S", BIT, 100 * 3 + 1)
            tryParseXgkTag "S3"     === None

        [<Test>]
        member _.``Test Abbreviated``() =
            tryParseXgkTagAbbreviated "P0" true  === Some("P", BIT, 0)
            tryParseXgkTagAbbreviated "P0" false === Some("P", WORD, 0)
            tryParseXgkTagAbbreviated "P1" true  === Some("P", BIT, 1)
            tryParseXgkTagAbbreviated "P1" false === Some("P", WORD, 16)
            tryParseXgkTagAbbreviated "PA" true  === Some("P", BIT, A)
            tryParseXgkTagAbbreviated "PA" false === None

            tryParseXgkTagAbbreviated "P00"     true  === Some("P", BIT, 0)
            tryParseXgkTagAbbreviated "P000"    true  === Some("P", BIT, 0)
            tryParseXgkTagAbbreviated "P0000"   true  === Some("P", BIT, 0)
            tryParseXgkTagAbbreviated "P00000"  true  === Some("P", BIT, 0)
            tryParseXgkTagAbbreviated "P000009" true  === None

            tryParseXgkTagAbbreviated "P00"     false  === Some("P", WORD, 0)
            tryParseXgkTagAbbreviated "P000"    false  === Some("P", WORD, 0)
            tryParseXgkTagAbbreviated "P0000"   false  === Some("P", WORD, 0)
            tryParseXgkTagAbbreviated "P00000"  false  === None

            LsXgkTagParser.Parse ("P0", true) === ("P", BIT, 0)
            LsXgkTagParser.Parse ("PA", false) === None
