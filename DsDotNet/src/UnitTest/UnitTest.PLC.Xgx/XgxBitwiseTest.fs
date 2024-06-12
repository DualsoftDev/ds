namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS

type XgxBitwiseTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Bitwise operation test`` () =
        let storages = Storages()

        // XGK 에서는 8byte int type (LWORD) 를 지원하지 않음
        let eightByteCodes = """
uint64  ul1 = 8UL &&& 255UL;
uint64  ul2 = 8UL ||| 255UL;
uint64  ul3 = 8UL ^^^ 255UL;
uint64  ul4 =     ~~~ 255UL;
$ul1 = 8UL &&& 255UL;
$ul2 = 8UL ||| 255UL;
$ul3 = 8UL ^^^ 255UL;
$ul4 =     ~~~ 255UL;
"""

        let code = $"""
bool truth = 8 &&& 255 == 8;
bool falsy = 8 &&& 255 == 3;
uint8   bn1 = 8uy &&& 255uy;
uint8   bn2 = 8uy ||| 255uy;
uint8   bn3 = 8uy ^^^ 255uy;
uint8   bn4 =     ~~~ 255uy;
uint16  sn1 = 8us &&& 255us;
uint16  sn2 = 8us ||| 255us;
uint16  sn3 = 8us ^^^ 255us;
uint16  sn4 =     ~~~ 255us;
uint32  un1 = 8u  &&& 255u;
uint32  un2 = 8u  ||| 255u;
uint32  un3 = 8u  ^^^ 255u;
uint32  un4 =     ~~~ 255u;

$truth = 8 &&& 255 == 8;
$falsy = 8 &&& 255 == 3;
$bn1 = 8uy &&& 255uy;
$bn2 = 8uy ||| 255uy;
$bn3 = 8uy ^^^ 255uy;
$bn4 =     ~~~ 255uy;
$sn1 = 8us &&& 255us;
$sn2 = 8us ||| 255us;
$sn3 = 8us ^^^ 255us;
$sn4 =     ~~~ 255us;
$un1 = 8u  &&& 255u;
$un2 = 8u  ||| 255u;
$un3 = 8u  ^^^ 255u;
$un4 =     ~~~ 255u;
{ if xgx = XGI then eightByteCodes else ""}

uint32 un999 = 0u;
$un999 = ~~~ ((8u &&& 255u) ||| ~~~(8u ^^^ 255u));
"""
        let f = getFuncName()
        let doit() =
            let statements = parseCodeForWindows storages code
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        match xgx with
        | XGI -> doit()
        | XGK -> doit |> ShouldFailWithSubstringT "XGK Bitwise operator not supported"
        | _ -> failwith "Not supported runtime target"


type XgiBitwiseTest() =
    inherit XgxBitwiseTest(XGI)
    [<Test>] member __.``Bitwise operation test`` () = base.``Bitwise operation test``()


type XgkBitwiseTest() =
    inherit XgxBitwiseTest(XGK)
    [<Test>] member __.``Bitwise operation test`` () = base.``Bitwise operation test``()
