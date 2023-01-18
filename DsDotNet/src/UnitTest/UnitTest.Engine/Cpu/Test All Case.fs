namespace T.CPU

open NUnit.Framework

open T
open Engine.Core
open Engine.Common.FS
open Engine.CodeGenCPU
open PLC.CodeGen.LSXGI
open System.IO
open System.Globalization

type TestAllCase() =
    inherit EngineTestBaseClass()
    
    let t = CpuTestSample()
    let projectDir =
        let src = __SOURCE_DIRECTORY__
        let key = @"UnitTest\UnitTest.PLC.Xgi"
        let tail = src.IndexOf(key) + key.Length
        src.Substring(0, tail)
    let xmlDir = Path.Combine(projectDir, "XgiXmls")

    let saveTestResult testFunctionName (xml:string) =
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText($@"{xmlDir}\{testFunctionName}.xml", crlfXml)


    [<Test>]  //<kwak> help rung 만들때 안되네요 Tag는 임시로 넘겼는데
    member __.``XXXXXXXXXXXXXXX Test All Case`` () =
        let stg = Storages()
        Runtime.Target <- XGI
        let result = Cpu.LoadStatements(t.Sys, stg).Head().Value //Active 기준으로 나머지 Tail도 출력필요 //test ahn
        let xml = LsXGI.generateXml stg  result
        saveTestResult (get_current_function_name()) xml
        result === result
