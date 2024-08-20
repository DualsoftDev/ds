namespace T.Comparison
open T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS
open PLC.CodeGen.Common

type XgxComparisonTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``COMP double test`` () =
        let storages = Storages()
        let code = """
            double pi = 3.14;         // LREAL type 으로 변환됨
            double eu = 2.718;
            bool b1 = $pi > $eu;
            bool b2 = $pi < $eu;
            bool b3 = $pi == $eu;
            bool b4 = $pi == $eu;   
            bool b5 = $pi != $eu;     // 확장 notation. "<>"
            bool b6 = $pi <> $eu;
            bool b7 = $pi >= $eu;
            bool b8 = $pi <= $eu;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)

        ["pi"; "eu" ] @ [ for i in [1..8] -> $"b{i}" ]
        |> List.iter (
            fun key ->
                let var = storages[key]
                tracefn "%s %s=%s" var.DataType.Name key var.Address)

        let pi, eu = storages.["pi"], storages.["eu"]
        pi.DataType === typeof<double>
        eu.DataType === typeof<double>
        if xgx = XGK then
            let addrPi = tryParseXGKTag(pi.Address).Value
            let addrEu = tryParseXGKTag(eu.Address).Value
            abs(addrEu.BitOffset - addrPi.BitOffset) === 64 

        x.saveTestResult f xml

    member x.``COMP int16 test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;             // INT type 으로 변환됨
            int16 nn2 = 2s;
            uint16 unn1 = 1us;
            uint16 unn2 = 2us;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 == $nn2;
             bool b4 = $nn1 == $nn2;
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;
            bool b9 = 1s > 2s;
            bool b10 = 1s < 2s;
            bool b11 = 1s == 2s;
            bool b12 = 1s <> 2s;


            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 == $unn2;
             bool ub4 = $unn1 == $unn2;  
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
            bool ub9 = 1us > 2us;
            bool ub10 = 1us < 2us;
            bool ub11 = 1us == 2us;
            bool ub12 = 1us <> 2us;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)

        let nn1, unn1 = storages.["nn1"], storages.["unn1"]
        nn1.DataType === typeof<int16>
        unn1.DataType === typeof<uint16>
        tracefn "%s\n" unn1.Address
        //eu.Address === pi.Address + 2

        x.saveTestResult f xml

    member x.``COMP int32 test`` () =
        let storages = Storages()
        let code = """
            int32 nn1 = 1;             // DINT type 으로 변환됨
            int32 nn2 = 2;
            uint32 unn1 = 1u;
            uint32 unn2 = 2u;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 == $nn2;
            bool b4 = $nn1 == $nn2;   
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;
            bool b9 = 1 > 2;
            bool b10 = 1 < 2;
            bool b11 = 1 == 2;
            bool b12 = 1 <> 2;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 == $unn2;
            bool ub4 = $unn1 == $unn2;  
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
            bool ub9 = 1 > 2;
            bool ub10 = 1 < 2;
            bool ub11 = 1 == 2;
            bool ub12 = 1 <> 2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)

        let nn1, nn2, unn1 = storages.["nn1"], storages.["nn2"], storages.["unn1"]
        nn1.DataType === typeof<int32>
        unn1.DataType === typeof<uint32>
        if xgx = XGK then
            let addrNn1 = tryParseXGKTag(nn1.Address).Value
            let addrNn2 = tryParseXGKTag(nn2.Address).Value
            addrNn2.BitOffset === addrNn1.BitOffset + 32 

        x.saveTestResult f xml

    member x.``COMP int64 test`` () =
        let storages = Storages()
        let code = """
            int64 nn1 = 1L;             // LINT type 으로 변환되어야 함.  XGK 에서는 LINT 가 지원되지 않음.
            int64 nn2 = 2L;
            uint64 unn1 = 1UL;
            uint64 unn2 = 2UL;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 == $nn2;
            bool b4 = $nn1 == $nn2;   
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;
            bool b9 = 1L > 2L;
            bool b10 = 1L < 2L;
            bool b11 = 1L == 2L;
            bool b12 = 1L <> 2L;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 == $unn2;
            bool ub4 = $unn1 == $unn2;   
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
            bool ub9 = 1UL > 2UL;
            bool ub10 = 1UL < 2UL;
            bool ub11 = 1UL == 2UL;
            bool ub12 = 1UL <> 2UL;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let test =
            fun () -> 
                let xml = x.generateXmlForTest f storages (map withNoComment statements)
                let nn1, unn1 = storages.["nn1"], storages.["unn1"]
                nn1.DataType === typeof<int64>
                unn1.DataType === typeof<uint64>
                tracefn "%s\n" unn1.Address
                x.saveTestResult f xml
        match xgx with
        | XGI -> test()
        | XGK -> test |> ShouldFailWithSubstringT "XGK does not support int64 types"
        | _ -> failwith "Not supported plc type"

    member x.``COMP int8 test`` () =
        let storages = Storages()
        let code = """
            int8 nn1 = 1y;              // SINT type 으로 변환됨
            int8 nn2 = 2y;
            uint8 unn1 = 1uy;
            uint8 unn2 = 2uy;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 == $nn2;
            bool b4 = $nn1 == $nn2;   
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;
            bool b9 = 1s > 2s;
            bool b10 = 1y < 2y;
            bool b11 = 1y == 2y;
            bool b12 = 1y <> 2y;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 == $unn2;
            bool ub4 = $unn1 == $unn2;
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;

            bool ub9 = 1uy > 2uy;
            bool ub10 = 1uy < 2uy;
            bool ub11 = 1uy == 2uy;
            bool ub12 = 1uy <> 2uy;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()

        let test =
            fun () -> 
                let xml = x.generateXmlForTest f storages (map withNoComment statements)
                let nn1, unn1 = storages.["nn1"], storages.["unn1"]

                nn1.DataType === typeof<sbyte>
                unn1.DataType === typeof<byte>
                tracefn "%s\n" unn1.Address
                x.saveTestResult f xml
        match xgx with
        | XGI -> test()
        | XGK -> test |> ShouldFailWithSubstringT "not supported in XGK"
        | _ -> failwith "Not supported plc type"

    member x.``COMP single test`` () =
        let storages = Storages()
        let code = """
            single pi = 3.14f;        // REAL type 으로 변환됨
            single eu = 2.718f;
            bool b1 = $pi > $eu;
            bool b2 = $pi < $eu;
            bool b3 = $pi == $eu;
            bool b4 = $pi == $eu;   
            bool b5 = $pi != $eu;     // 확장 notation. "<>"
            bool b6 = $pi <> $eu;
            bool b7 = $pi >= $eu;
            bool b8 = $pi <= $eu;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)

        ["pi"; "eu" ] @ [ for i in [1..8] -> $"b{i}" ]
        |> List.iter (
            fun key ->
                let var = storages[key]
                tracefn "%s %s=%s" var.DataType.Name key var.Address)

        let pi, eu = storages.["pi"], storages.["eu"]
        pi.DataType === typeof<single>
        eu.DataType === typeof<single>
        if xgx = XGK then
            let addrPi = tryParseXGKTag(pi.Address).Value
            let addrEu = tryParseXGKTag(eu.Address).Value
            abs(addrEu.BitOffset - addrPi.BitOffset) === 32 

        x.saveTestResult f xml




//[<Collection("SerialXgxFunctionTest")>]
type XgiComparisonTest() =
    inherit XgxComparisonTest(XGI)

    [<Test>] member __.``COMP double test`` () = base.``COMP double test``()
    [<Test>] member __.``COMP int16 test`` () = base.``COMP int16 test``()
    [<Test>] member __.``COMP int32 test`` () = base.``COMP int32 test``()
    [<Test>] member __.``COMP int64 test`` () = base.``COMP int64 test``()
    [<Test>] member __.``COMP int8 test`` () = base.``COMP int8 test``()
    [<Test>] member __.``COMP single test`` () = base.``COMP single test``()

type XgkComparisonTest() =
    inherit XgxComparisonTest(XGK)

    [<Test>] member __.``COMP double test`` () = base.``COMP double test``()
    [<Test>] member __.``COMP int16 test`` () = base.``COMP int16 test``()
    [<Test>] member __.``COMP int32 test`` () = base.``COMP int32 test``()
    [<Test>] member __.``COMP int64 test`` () = base.``COMP int64 test``()
    [<Test>] member __.``COMP int8 test`` () = base.``COMP int8 test``()
    [<Test>] member __.``COMP single test`` () = base.``COMP single test``()




