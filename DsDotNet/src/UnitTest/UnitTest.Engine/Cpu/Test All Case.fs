namespace T.CPU

open System.IO
open System.Linq
open NUnit.Framework

open T
open Engine.Core
open Engine.Common.FS
open Engine.CodeGenCPU
open PLC.CodeGen.LSXGI

type TestAllCase() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    let generateXmlForTest projName globalStorages localStorages (pous:PouGen seq): string =
        let getXgiPOUParams (pouGen:PouGen) =
                    let pouParams:XgiPOUParams = {
                        /// POU name.  "DsLogic"
                        POUName = pouGen.ToSystem().Name
                        /// POU container task name
                        TaskName = pouGen.TaskName()
                                /// POU ladder 최상단의 comment
                        Comment = "DS Logic for XGI"
                        LocalStorages = localStorages
                        CommentedStatements = pouGen.CommentedStatements()
                    }
                    pouParams

        let projParams:XgiProjectParams = {
            ProjectName = projName
            GlobalStorages = globalStorages
            ExistingLSISprj = None
            POUs = pous.Select(getXgiPOUParams) |> Seq.toList
        }

        projParams.GenerateXmlString()

    let saveTestResult testFunctionName (xml:string) =
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        let myTemplate = Path.Combine($"{__SOURCE_DIRECTORY__}", "../../UnitTest.PLC.Xgi/XgiXmls")
        File.WriteAllText($@"{myTemplate}\{testFunctionName}.xml", crlfXml)


    [<Test>]
    member __.``XXXXXXXXXXXXXX Test All Case`` () =
        let globalStorage = Storages()
        let localStorage = Storages()
        Runtime.Target <- XGI
        let result = Cpu.LoadStatements(t.Sys, globalStorage)

        let f = get_current_function_name()
        let xml = generateXmlForTest f globalStorage localStorage result
        saveTestResult f xml
        result === result
