namespace T

open NUnit.Framework

open Dual.Common.UnitTest.FS
open Dual.PLC.TagParser.FS

[<AutoOpen>]
module LsXgiTagParserTestModule =

    [<TestFixture>]
    type LsXgiTagParserTest() =
        let BIT, BYTE, WORD, DWORD, LWORD = 1, 8, 16, 32, 64
        let FILE = 32

        [<Test>]
        member _.``Test XGI``() =

            tryParseXgiTag "%IX0"  === Some("I", BIT, BIT * 0)
            tryParseXgiTag "%IX1"  === Some("I", BIT, BIT * 1)
            tryParseXgiTag "%IX10" === Some("I", BIT, BIT * 10)
            tryParseXgiTag "%IX77" === Some("I", BIT, BIT * 77)
            LsXgiTagParser.Parse "%IX0" === ("I", BIT, 0)

            tryParseXgiTag "%IX1.2.63" ===& Some("I", BIT,   (1024) * 1 + (64) * 2  + 63 * 1) === Some("I", BIT, 1215)
            tryParseXgiTag "%IX1.2.64" === None // bit 이므로 bit 지정은 [0..63] 까지   1 * 64 = 64
            tryParseXgiTag "%IB1.2.6"  ===& Some("I", BYTE,  (1024) * 1 + (64) * 2  + 6 * 8) === Some("I", BYTE, 1200)
            tryParseXgiTag "%IB1.2.7"  ===& Some("I", BYTE,  (1024) * 1 + (64) * 2  + 7 * 8) === Some("I", BYTE, 1208)
            tryParseXgiTag "%IB1.2.8"  === None // byte 이므로 byte 지정은 [0..7] 까지   8 * 8 = 64
            tryParseXgiTag "%IW1.2.2"  === Some("I", WORD,  (1024) * 1 + (64) * 2  + 2 * 16)
            tryParseXgiTag "%IW1.2.3"  === Some("I", WORD,  (1024) * 1 + (64) * 2  + 3 * 16)
            tryParseXgiTag "%IW1.2.4"  === None // Word 이므로 Word 지정은 [0..3] 까지   16 * 4 = 64
            tryParseXgiTag "%ID1.2.0"  === Some("I", DWORD, (1024) * 1 + (64) * 2  + 0 * 32)
            tryParseXgiTag "%ID1.2.1"  === Some("I", DWORD, (1024) * 1 + (64) * 2  + 1 * 32)
            tryParseXgiTag "%ID1.2.2"  === None // DWord 이므로 DWord 지정은 [0..1] 까지 32 * 2 = 64
            tryParseXgiTag "%IL1.2.0"  === Some("I", LWORD, (1024) * 1 + (64) * 2  + 0 * 64)
            tryParseXgiTag "%IL1.2.1"  === None // LWord 이므로 LWord 지정은 [0..0] 까지   64 * 1 = 64

            // safety 주소 체계는 확인 필요.  일단은 safety 아닌 pair 기준으로 작성
            tryParseXgiTag "%ISB2.3.7"   === Some("IS", BYTE, (1024) * 2 + (64) * 3 + 7 * 8)      // safety
            tryParseXgiTag "%ISB2.3.42"  === None

            tryParseXgiTag "%IX0.0.40"  === Some("I", BIT,  0 * 1024 + 0 * 64  + 40)


            tryParseXgiTag "%IB3.7"   === Some("I", BIT,  BYTE  * 3 + BIT * 7)
            tryParseXgiTag "%IB3.8"   === None
            tryParseXgiTag "%IW3.15"  === Some("I", BIT,  WORD  * 3 + BIT * 15)
            tryParseXgiTag "%IW3.16"  === None
            tryParseXgiTag "%ID3.31"  === Some("I", BIT, DWORD * 3 + BIT * 31)
            tryParseXgiTag "%ID3.32"  === None
            tryParseXgiTag "%IL3.63"  === Some("I", BIT, LWORD * 3 + BIT * 63)
            tryParseXgiTag "%IL3.64"  === None


            tryParseXgiTag "%IX1" === Some("I", BIT,    BIT   * 1)
            tryParseXgiTag "%IB1" === Some("I", BYTE,   BYTE  * 1)
            tryParseXgiTag "%IW1" === Some("I", WORD,   WORD  * 1)
            tryParseXgiTag "%ID1" === Some("I", DWORD,  DWORD * 1)
            tryParseXgiTag "%IL1" === Some("I", LWORD,  LWORD * 1)

            tryParseXgiTag "%IX2" === Some("I", BIT,    BIT   * 2)
            tryParseXgiTag "%IB2" === Some("I", BYTE,   BYTE  * 2)
            tryParseXgiTag "%IW2" === Some("I", WORD,   WORD  * 2)
            tryParseXgiTag "%ID2" === Some("I", DWORD,  DWORD * 2)
            tryParseXgiTag "%IL2" === Some("I", LWORD,  LWORD * 2)


            tryParseXgiTag "%UX0.0.511"  === Some("U", BIT,  BIT * 511)
            tryParseXgiTag "%UX0.0.512"  === None
            tryParseXgiTag "%UX0.15.511" === Some("U", BIT,  (512 * 16) * 0 + (512) * 15 + BIT * 511)
            tryParseXgiTag "%UX0.16.512" === None

            tryParseXgiTag "%UB0.0.63"   === Some("U", BYTE, (512 * 16) * 0 + (512) * 0 + 63 * 8)
            tryParseXgiTag "%UB0.0.64"   === None
            tryParseXgiTag "%UB1.2.3"    === Some("U", BYTE, (512 * 16) * 1 + (512) * 2 + 3 * 8)


            tryParseXgiTag "%MX11008"    === Some("M", BIT, BIT * 11008)


            tryParseXgiTag "%MX1768"  === Some("M", BIT, BIT * 1768)

            for invalid in ["%IMF1"; "%IX3.-1"; "IX3.1"] do
                tryParseXgiTag invalid === None
                LsXgiTagParser.Parse invalid === null
