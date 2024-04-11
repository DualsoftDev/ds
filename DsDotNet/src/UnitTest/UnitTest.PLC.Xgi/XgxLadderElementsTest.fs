namespace T

open NUnit.Framework
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine.ElementType
open System.Security
open Engine.Core
open Engine.Parser.FS
open Dual.UnitTest.Common.FS

type XgiLadderElementTest() =
    inherit XgiTestBaseClass()

    let span width = width*3

    [<Test>]
    member __.``Local var test``() =
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
            typedefof<int64>
            typedefof<uint64>
            typedefof<string>
        ]

        let symbolInfos = [
            for t in testSymbolTypes do
                let plcType = systemTypeToXgiTypeName t
                let comment = $"{plcType} <- {t.Name}"
                let name = $"my{t.Name}"
                let initValueHolder:BoxedObjectHolder = {Object=null}
                XGITag.createSymbolInfo name comment plcType (int Variable.Kind.VAR)  initValueHolder
        ]

        let symbolsLocalXml = XGITag.generateLocalSymbolsXml TestRuntimeTargetType symbolInfos

        let rungsXml = ""   //generateRungs prologComments commentedStatements
        let symbolsGlobalXml = """<GlobalVariable Version="Ver 1.0" Count="0"/>"""
        let xml = wrapWithXml TestRuntimeTargetType rungsXml symbolsLocalXml symbolsGlobalXml None
        saveTestResult (getFuncName()) xml



    [<Test>]
    member __.``Local var with init test`` () =
        let storages = Storages()
        let code = """
            bool    mybool   = false;
            single  mysingle = 0.1f;
            double  mydouble = 3.14;
            sbyte   mysbyte  = 1y;
            char    mychar   = 'a';
            byte    mybyte   = 2uy;
            int16   myint16  = 16s;
            uint16  myuint16 = 16us;
            int32   myint32  = 32;
            uint32  myuint32 = 32u;
            int64   myint64  = 64L;
            uint64  myuint64 = 64UL;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Local var with init string err test`` () =
        let storages = Storages()
        let code = """
            string  mystring = "hello";     // not working for string
"""
        let statements = parseCode storages code
        let f = getFuncName()
        (fun () ->  XgiFixtures.generateXmlForTest f storages (map withNoComment statements) |> ignore) |> ShouldFail



    [<Test>]
    member __.``Local var with comment and init test`` () =
        let storages = Storages()
        let code = """
            bool    mybool   = false;
            int16   myint16  = 16s;
            int32   myint32  = 32;
            int64   myint64  = 64L;
"""
        let statements = parseCode storages code
        storages["mybool"].Comment <- "mybool comment"
        storages["myint16"].Comment <- "myint16 comment <> ! +-*/"
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml
