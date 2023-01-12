namespace T

open NUnit.Framework
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine.ElementType
open System.Security
open Engine.Core

type XgiLadderElementTest() =
    inherit XgiTestClass()

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
                let plcType = systemTypeNameToXgiTypeName t.Name
                let comment = $"{plcType} <- {t.Name}"
                let name = $"my{t.Name}"
                let initValueHolder:BoxedObjectHolder = {Object=null}
                XGITag.createSymbolInfo name comment plcType initValueHolder
        ]

        let symbolsLocalXml = XGITag.generateLocalSymbolsXml symbolInfos


        //let xml = generateXGIXmlFromStatement [] [] xgiSymbols unusedTags existingLSISprj
        //xml

        let rungsXml = ""   //generateRungs prologComments commentedStatements
        let symbolsGlobalXml = """<GlobalVariable Version="Ver 1.0" Count="0"/>"""
        let xml = wrapWithXml rungsXml symbolsLocalXml symbolsGlobalXml None
        saveTestResult (get_current_function_name()) xml



