namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS

type XgxBitwiseTest(xgx:HwCPU) =
    inherit XgxTestBaseClass(xgx)

    member x.``Bitwise shift test`` () =
        let code = """
uint8 bn5 = 0uy;
$bn5 = 1uy <<< 1;"""
        let storages, statements = code |> x.TestCode (getFuncName())
        ()

    member x.``Bitwise operation test1`` () =
        let code = """
int nn1 = 8 &&& 255;
$nn1 = 9 &&& 255;
"""
        let storages, statements = code |> x.TestCode (getFuncName())
        storages["nn1"].BoxedValue === 8
        statements[1].Do()
        storages["nn1"].BoxedValue === 9

    member x.``Bitwise operation test2`` () =
        let code = """
bool truth = 8 &&& 255 == 8;
$truth = 9 &&& 255 == 8;
"""
        let storages, statements = code |> x.TestCode (getFuncName())
        storages["truth"].BoxedValue === true
        statements[1].Do()
        storages["truth"].BoxedValue === false


    member x.``Bitwise operation test`` () =
        // XGK 에서는 8byte int type (LWORD) 를 지원하지 않음
        let eightByteCodes = """
uint64  ul1 = 8UL &&& 255UL;
uint64  ul2 = 8UL ||| 255UL;
uint64  ul3 = 8UL ^^^ 255UL;
uint64  ul4 =     ~~~ 255UL;
uint64  ul5 = 1UL <<< 1;
$ul1 = 8UL &&& 255UL;
$ul2 = 8UL ||| 255UL;
$ul3 = 8UL ^^^ 255UL;
$ul4 =     ~~~ 255UL;
$ul5 = 1UL <<< 1;
"""
        let byteCodes = """
uint8   bn1 = 8uy &&& 255uy;
uint8   bn2 = 8uy ||| 255uy;
uint8   bn3 = 8uy ^^^ 255uy;
uint8   bn4 =     ~~~ 255uy;
uint8   bn5 = 1uy <<< 1;
$bn1 = 8uy &&& 255uy;
$bn2 = 8uy ||| 255uy;
$bn3 = 8uy ^^^ 255uy;
$bn4 =     ~~~ 255uy;
$bn5 = 1uy  <<< 1;
"""

        let code = $"""
bool truth = 8 &&& 255 == 8;
bool falsy = 8 &&& 255 == 3;
$truth = 8 &&& 255 == 8;
$falsy = 8 &&& 255 == 3;

{ if xgx = XGI then byteCodes else ""}

uint16  sn1 = 8us &&& 255us;
uint16  sn2 = 8us ||| 255us;
uint16  sn3 = 8us ^^^ 255us;
uint16  sn4 =     ~~~ 255us;
uint16  sn5 = 1us  <<< 1;
$sn1 = 8us &&& 255us;
$sn2 = 8us ||| 255us;
$sn3 = 8us ^^^ 255us;
$sn4 =     ~~~ 255us;
$sn5 = 1us  <<< 1;


uint32  un1 = 8u  &&& 255u;
uint32  un2 = 8u  ||| 255u;
uint32  un3 = 8u  ^^^ 255u;
uint32  un4 =     ~~~ 255u;
uint32  un5 = 1u  <<< 1;
$un1 = 8u  &&& 255u;
$un2 = 8u  ||| 255u;
$un3 = 8u  ^^^ 255u;
$un4 =     ~~~ 255u;
$un5 = 1u  <<< 1;

{ if xgx = XGI then eightByteCodes else ""}

uint32 un999 = 0u;
$un999 = ~~~ ((8u &&& 255u) ||| ~~~(8u ^^^ 255u));
"""
        let f = getFuncName()
        let doit() =  code |> x.TestCode f
        match xgx with
        | XGI ->
            let storages, statements = doit()
            storages["truth"].BoxedValue === true
            storages["un4"]  .BoxedValue === ~~~255u
            storages["falsy"].BoxedValue === false

            ()
        | XGK -> (doit >> ignore) |> ShouldFailWithSubstringT "XGK Bitwise operator not supported"
        | _ -> failwith "Not supported runtime target"


type XgiBitwiseTest() =
    inherit XgxBitwiseTest(XGI)
    [<Test>] member __.``Bitwise operation test`` () = base.``Bitwise operation test``()
    [<Test>] member __.``Bitwise operation test1`` () = base.``Bitwise operation test1``()
    [<Test>] member __.``Bitwise operation test2`` () = base.``Bitwise operation test2``()
    [<Test>] member __.``Bitwise shift test`` () = base.``Bitwise shift test``()


type XgkBitwiseTest() =
    inherit XgxBitwiseTest(XGK)
    [<Test>] member __.``Bitwise operation test`` () = base.``Bitwise operation test``()
