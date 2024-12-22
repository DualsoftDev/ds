namespace T.Rung
open T

open NUnit.Framework
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open Engine.Core
open Engine.Parser.FS
open Dual.Common.UnitTest.FS

type XgxLadderElementTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let span width = width*3

    member x.``Local var test``() =
        let testSymbolTypes = [
            typedefof<bool>
            typedefof<single>
            typedefof<double>
            typedefof<sbyte>
            typedefof<char>
            typedefof<byte>
            typedefof<int16>
            typedefof<uint16>
            typedefof<int32>
            typedefof<uint32>
            if xgx = XGI then
                typedefof<int64>
                typedefof<uint64>
            typedefof<string>
        ]

        let symbolInfos = [
            let counter = counterGenerator 0
            for t in testSymbolTypes do
                let plcType = systemTypeToXgiTypeName t
                let comment = $"{plcType} <- {t.Name}"
                let name = $"my{t.Name}"
                let initValueHolder:BoxedObjectHolder = {Object=null}
                let symbolInfo = XGITag.createSymbolInfo name comment plcType (int Variable.Kind.VAR)  initValueHolder
                match xgx with
                | XGI -> symbolInfo
                | XGK -> { symbolInfo with DevicePos = counter()}
                | _ -> failwith "Not supported plc type"

        ]

        let rungsXml = ""   //generateRungs prologComments commentedStatements
        let symbolsGlobalXml = """<GlobalVariable Version="Ver 1.0" Count="0"/>"""
        let xml =
            let prjParam = getXgxProjectParams xgx (getFuncName())
            wrapWithXml prjParam rungsXml symbolInfos symbolsGlobalXml None
        match xgx with
        | XGI -> x.saveTestResult (getFuncName()) xml
        | _ -> ()

    member x.``Local var with comment and init test`` () =
        let storages = Storages()
        let code = """
            bool    mybool   = false;
            int16   myint16  = 16s;
            int32   myint32  = 32;
"""
        let code =
            match xgx with
            | XGK -> code
            | XGI -> code + """
            int64   myint64  = 64L;
"""
            | _ -> failwith "Not supported plc type"

        let statements = parseCodeForWindows storages code
        storages["mybool"].Comment <- "mybool comment"
        storages["myint16"].Comment <- "myint16 comment <> ! +-*/"
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml
        ///todo: string type 처리 구현 필요
    member x.``Local var with init string test`` () =
        let storages = Storages()
        let code = """
            string  mystring = "hello";     // not working for string
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Local var with init test`` () =
        let storages = Storages()
        let code = """
            bool    mybool   = false;
            single  mysingle = 0.1f;
            double  mydouble = 3.14;
            sbyte   mysbyte  = 1y;
            int16   myint16  = 16s;
            uint16  myuint16 = 16us;
            int32   myint32  = 32;
            uint32  myuint32 = 32u;
"""
        let code =
            match xgx with
            | XGK -> code
            | XGI -> code + """
            int64   myint64  = 64L;
            uint64  myuint64 = 64UL;
            char    mychar   = 'a';
            byte    mybyte   = 2uy;
"""
            | _ -> failwith "Not supported plc type"
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml





type XgiLadderElementTest() =
    inherit XgxLadderElementTest(XGI)
    [<Test>] member x.``Local var test``() = base.``Local var test``()
    [<Test>] member x.``Local var with comment and init test`` () = base.``Local var with comment and init test``()
    [<Test>] member x.``Local var with init string err test`` () = base.``Local var with init string test``()
    [<Test>] member x.``Local var with init test`` () = base.``Local var with init test``()

type XgkLadderElementTest() =
    inherit XgxLadderElementTest(XGK)
    [<Test>] member x.``Local var test``() = base.``Local var test``()
    [<Test>] member x.``Local var with comment and init test`` () = base.``Local var with comment and init test``()
    [<Test>] member x.``Local var with init string err test`` () = base.``Local var with init string test``()
    [<Test>] member x.``Local var with init test`` () = base.``Local var with init test``()

