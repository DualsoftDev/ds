namespace T

open NUnit.Framework
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine.ElementType
open System.Security
open Engine.Core
open Engine.Parser.FS

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
                XGITag.createSymbolInfo name comment plcType initValueHolder
        ]

        let symbolsLocalXml = XGITag.generateLocalSymbolsXml symbolInfos

        let rungsXml = ""   //generateRungs prologComments commentedStatements
        let symbolsGlobalXml = """<GlobalVariable Version="Ver 1.0" Count="0"/>"""
        let xml = wrapWithXml rungsXml symbolsLocalXml symbolsGlobalXml None
        saveTestResult (get_current_function_name()) xml



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
            string  mystring = "hello";     // not working for string
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.tempGenerateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml
