namespace T

open NUnit.Framework
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine.ElementType

type XgiLadderElementTest() =
    inherit XgiTestClass()

    let span width = width*3

    [<Test>]
    member __.``Local var test``() =
        let name = "MySINT"
        let comment = name
        let device = ""
        let kindVar = int Variable.Kind.VAR
        let plcType = "SINT"
        let symbolInfo = XGITag.createSymbol name comment device kindVar "" plcType
        let symbolsLocalXml = XGITag.generateSymbolVars ([symbolInfo], false)


        //let xml = generateXGIXmlFromStatement [] [] xgiSymbols unusedTags existingLSISprj
        //xml

        let rungsXml = ""   //generateRungs prologComments commentedStatements
        let symbolsGlobalXml = """<GlobalVariable Version="Ver 1.0" Count="0"/>"""
        let xml = wrapWithXml rungsXml symbolsLocalXml symbolsGlobalXml None
        saveTestResult (get_current_function_name()) xml



