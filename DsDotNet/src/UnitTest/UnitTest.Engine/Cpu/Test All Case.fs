namespace T.CPU

open NUnit.Framework

open T
open Engine.Core
open Engine.Common.FS
open Engine.CodeGenCPU
open PLC.CodeGen.LSXGI
open System.IO
open System.Globalization
open Microsoft.Office.Interop.Excel

type TestAllCase() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    let projectDir =
        let src = __SOURCE_DIRECTORY__
        let key = @"UnitTest\UnitTest.PLC.Xgi"
        let tail = src.IndexOf(key) + key.Length
        src.Substring(0, tail)
    let xmlDir = Path.Combine(projectDir, "XgiXmls")

    let generateXml globalStorages localStorages (statements:CommentedStatement list) : string =
        let pouParams:XgiPOUParams = {
            /// POU name.  "DsLogic"
            POUName = "DsLogic"
            /// POU container task name
            TaskName = "스캔 프로그램"
            /// POU ladder 최상단의 comment
            Comment = "DS Logic for XGI"
            LocalStorages = localStorages
            CommentedStatements = statements
        }
        let projParams:XgiProjectParams = {
            GlobalStorages = globalStorages
            ExistingLSISprj = None
            POUs = [pouParams]
        }

        projParams.GenerateXmlString()
    let saveTestResult testFunctionName (xml:string) =
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText($@"{xmlDir}\{testFunctionName}.xml", crlfXml)


    [<Test>]  //<kwak> help rung 만들때 안되네요 Tag는 임시로 넘겼는데
    member __.``XXXXXXXXXXXXXXX Test All Case`` () =
        let globalStorage = Storages()
        let localStorage = Storages()
        Runtime.Target <- XGI
        let result = Cpu.LoadStatements(t.Sys, globalStorage).Head().Value //Active 기준으로 나머지 Tail도 출력필요 //test ahn
        let xml = generateXml globalStorage localStorage  result
        saveTestResult (get_current_function_name()) xml
        result === result
