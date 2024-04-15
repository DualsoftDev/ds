namespace T

open Dual.UnitTest.Common.FS
open System.IO
open System.Globalization

open NUnit.Framework

open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.LS
open Engine.Cpu
open Engine.Parser.FS


[<AutoOpen>]
module XgxGenerationTestModule =
    let projectDir =
        let src = __SOURCE_DIRECTORY__
        let key = "UnitTest.PLC.Xgx"
        let index = src.LastIndexOf key
        src.Substring(0, index + key.Length)

    let generateVariableDeclarationSeq (typ:string) (varNamePrefix:string) (initialValueSetter: int -> string) (start: int) (count: int) =
        seq {
            for i in start .. start + count - 1 do
                yield sprintf "%s %s%d = %s;" typ varNamePrefix i (initialValueSetter i)   // e.g . "int16 n01 = 1s;"
        }

    let generateVariableDeclarations (typ:string) (varNamePrefix:string) (initialValueSetter: int -> string) (start: int) (count: int) =
        generateVariableDeclarationSeq typ varNamePrefix initialValueSetter start count |> String.concat "\n"

    /// bool x01 = createTag("%IX0.0", false); 등과 같은 항목을 반복 생성한다.
    let generateBitTagVariableDeclarationSeq (xgx:PlatformTarget) (start: int) (count: int) =
        seq {
            for i in start .. start + count - 1 do
                let tag =
                    match xgx with
                    | XGI -> sprintf "%%IX0.%d.%d" (i / 16) (i % 16)
                    | XGK -> sprintf "P%05X" i
                    | _ -> failwith "Not supported runtime target"
                yield sprintf "bool x%02d = createTag(\"%s\", false);" i tag
        } 
    let generateBitTagVariableDeclarations (xgx:PlatformTarget) (start: int) (count: int) =
        generateBitTagVariableDeclarationSeq xgx start count |> String.concat "\n"

    let generateInt16VariableDeclarations (start: int) (count: int) =
        generateVariableDeclarations "int16" "nn" (fun i -> sprintf "%ds" i) start count

    let generateLargeVariableDeclarations (xgx:PlatformTarget) =
        seq {
            yield! generateBitTagVariableDeclarationSeq xgx 0 40
            yield! generateVariableDeclarationSeq "int" "nn" (fun i -> sprintf "%d" i) 1 9
        } |> String.concat "\n"
        


module XgiGenerationTestModule =
    let private xmlDir = Path.Combine(projectDir, "Xgi/Xmls")
    let private xmlAnswerDir = Path.Combine(xmlDir, "Answers")

    let saveTestResult testFunctionName (xml:string) =
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText($@"{xmlDir}/{testFunctionName}.xml", crlfXml)
        let answerXml = File.ReadAllText($"{xmlAnswerDir}/{testFunctionName}.xml")
        System.String.Compare(answerXml, xml, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) === 0


module XgkGenerationTestModule =
    let private xmlDir = Path.Combine(projectDir, "Xgk/Xmls")
    let private xmlAnswerDir = Path.Combine(xmlDir, "Answers")

    let saveTestResult testFunctionName (xml:string) =
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText($@"{xmlDir}/{testFunctionName}.xml", crlfXml)
        let answerXml = File.ReadAllText($"{xmlAnswerDir}/{testFunctionName}.xml")
        System.String.Compare(answerXml, xml, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) === 0



[<AutoOpen>]
module XgxFixtures =

    let mutable runtimeTarget = WINDOWS
    let setRuntimeTarget(target:PlatformTarget) =
        let runtimeTargetBackup = target
        RuntimeDS.System <- sys
        ParserUtil.runtimeTarget <-target
        runtimeTarget <- target
        disposable { runtimeTarget <- runtimeTargetBackup }


    let private generateXmlForTest (xgx:PlatformTarget) projName (storages:Storages) (commentedStatements:CommentedStatement list) : string =
        tracefn <| $"IsDebugVersion={IsDebugVersion}, isInUnitTest()={isInUnitTest()}"

        //verify (RuntimeDS.Target = xgx)

        let globalStorages = storages
        let localStorages = Storages()

        let pouParams:XgxPOUParams = {
            /// POU name.  "DsLogic"
            POUName = "DsLogic"
            /// POU container task name
            TaskName = "Scan Program"
            /// POU ladder 최상단의 comment
            Comment = "DS Logic for XGI"
            LocalStorages = localStorages
            GlobalStorages = globalStorages
            CommentedStatements = commentedStatements
        }
        let prjParam:XgxProjectParams = {
            defaultXgxProjectParams with
                TargetType = xgx
                ProjectName = projName
                GlobalStorages = globalStorages
                POUs = [pouParams]
                RungCounter = counterGenerator 0 |> Some
        }

        prjParam.GenerateXmlString()

    let TestPlatformTarget = XGI
    [<AbstractClass>]
    type XgxTestBaseClass(xgx:PlatformTarget) =
        inherit TestBaseClass("EngineLogger")
        do
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()
            autoVariableCounter <- 0

        //let mutable runtimeTarget = RuntimeDS.Target
        let sys = DsSystem("testSys")
        [<SetUp>]
        member x.Setup () =
            //RuntimeDS.Target <- x.GetCurrentRuntimeTarget()
            RuntimeDS.System <- sys

        [<TearDown>]
        member __.TearDown () =
            //RuntimeDS.Target <- runtimeTarget
            RuntimeDS.System <- sys

        //abstract GetCurrentRuntimeTarget: unit -> PlatformTarget

        //override x.GetCurrentRuntimeTarget() = TestPlatformTarget

        member __.saveTestResult testFunctionName (xml:string) =
            match xgx with
            | XGI -> XgiGenerationTestModule.saveTestResult testFunctionName xml
            | XGK -> XgkGenerationTestModule.saveTestResult testFunctionName xml
            | _ -> failwith "Not supported runtime target"

        member __.generateXmlForTest = generateXmlForTest xgx
