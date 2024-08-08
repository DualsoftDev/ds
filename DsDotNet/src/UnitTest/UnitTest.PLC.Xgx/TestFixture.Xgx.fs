namespace T

open Dual.UnitTest.Common.FS
open Dual.Common.Base.FS
open System.IO
open System.Diagnostics
open System.Text
open System.Globalization

open NUnit.Framework

open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.LS
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

    /// bool x00 = createTag("%IX0.0.0", false); 등과 같은 항목을 반복 생성한다.
    /// varPrefix: e.g "x", addressPrefix: e.g. "%IX0."
    let generateBitTagVariableDeclarationSeq (xgx:PlatformTarget) (varPrefix:string) (addressPrefix:string) (start: int) (count: int) =
        seq {
            for i in start .. start + count - 1 do
                let tag =
                    match xgx with
                    | XGI when addressPrefix.StartsWith("MX") ->
                        sprintf "%%%s%d%d" addressPrefix (i / 16) (i % 16)
                    | XGI ->
                        sprintf "%%%s.%d.%d" addressPrefix (i / 16) (i % 16)
                    | XGK -> sprintf "%s%04d%X" addressPrefix (i / 16) (i % 16)
                    | _ -> failwith "Not supported runtime target"
                yield sprintf "bool %s%02d = createTag(\"%s\", false);" varPrefix (i-start) tag
        }

    let private getAddressPrefix (xgx:PlatformTarget) =
        match xgx with
        | XGI -> "IX0"
        | XGK -> "P"
        | _ -> failwith "Not supported runtime target"

    let generateBitTagVariableDeclarations (xgx:PlatformTarget) (start: int) (count: int) =
        let addressPrefix = getAddressPrefix xgx
        generateBitTagVariableDeclarationSeq xgx "x" addressPrefix start count |> String.concat "\n"

    /// bool varPrefix00 = createTag("%IX0.0.0", false); 등과 같은 항목을 반복 생성한다.
    let generateNamedBitTagVariableDeclarations (xgx:PlatformTarget) (varPrefix:string) (addressPrefix:string) (start: int) (count: int) =
        generateBitTagVariableDeclarationSeq xgx varPrefix addressPrefix start count |> String.concat "\n"

    let generateInt16VariableDeclarations (start: int) (count: int) =
        generateVariableDeclarations "int16" "nn" (fun i -> sprintf "%ds" i) start count

    let generateLargeVariableDeclarations (xgx:PlatformTarget) =
        let addressPrefix = getAddressPrefix xgx
        seq {
            yield! generateBitTagVariableDeclarationSeq xgx "x" addressPrefix 0 40
            yield! generateVariableDeclarationSeq "int32" "nn" (fun i -> sprintf "%d" i) 1 9
        } |> String.concat "\n"

    let internal saveXgxTestResult (xgx:PlatformTarget) (testFunctionName:string) (xml:string) =
        let xmlDir = Path.Combine(projectDir, $"{xgx}/Xmls")
        let xmlAnswerDir = Path.Combine(xmlDir, "Answers")
        File.WriteAllText($@"{xmlDir}/{testFunctionName}.xml", xml)
        let answerXml = File.ReadAllText($"{xmlAnswerDir}/{testFunctionName}.xml")
        //System.String.Compare(answerXml, xml, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) === 0
        answerXml === xml


[<AutoOpen>]
module XgxFixtures =

    let mutable runtimeTarget = WINDOWS, LS_XGK_IO
    let sys = DsSystem.Create4Test("")
    let setRuntimeTarget(target:PlatformTarget) =
        let runtimeTargetBackup = target, LS_XGK_IO
        RuntimeDS.System <- sys
        ParserUtil.runtimeTarget <-target
        runtimeTarget <- target, LS_XGK_IO
        disposable { runtimeTarget <- runtimeTargetBackup }

    let parseExpression4UnitTest (storages: Storages) (text: string) : IExpression =
        try
            let parser = createParser (text)
            let ctx = parser.expr ()
            let parserData = ParserData((WINDOWS), Storages(), None)

            createExpression parserData (defaultStorageFinder storages) ctx
        with exn ->
            failwith $"Failed to parse Expression: {text}\r\n{exn}" // Just warning.  하나의 이름에 '.' 을 포함하는 경우.  e.g "#seg.testMe!!!"

    /// Unit test 용 PLC XML 생성 함수.  실제 runtime 환경에서는 generateXmlXGX 사용
    let private generateXmlForTest (xgx:PlatformTarget) projName (storages:Storages) (commentedStatements:CommentedStatement list) : string =
        tracefn <| $"IsDebugVersion={IsDebugVersion}, isInUnitTest()={isInUnitTest()}"

        //verify (RuntimeDS.Target = xgx)

        let globalStorages = storages
        let localStorages = Storages()

        let pouParams:XgxPOUParams = {
            // POU name.  "DsLogic"
            POUName = "DsLogic"
            // POU container task name
            TaskName = "Scan Program"
            // POU ladder 최상단의 comment
            Comment = "DS Logic for XGI"
            LocalStorages = localStorages
            GlobalStorages = globalStorages
            CommentedStatements = commentedStatements
        }
        let prjParam:XgxProjectParams = {
            getXgxProjectParams xgx projName with
                GlobalStorages = globalStorages
                POUs = [pouParams]
        }

        prjParam.GenerateXmlString()

    let TestPlatformTarget = XGI



    [<AbstractClass>]
    type XgxTestBaseClass(xgx:PlatformTarget) =
        inherit TestClassWithLogger(Path.Combine($"{__SOURCE_DIRECTORY__}/App.config"), "UnitTestLogger")

        let sys = DsSystem.Create4Test("testSys")

        /// XML을 포맷팅하는 Node.js 스크립트를 실행한다.
        /// Exception 발생 할 경우 조치방법
        ///
        /// - Node.js 설치 (동작 확인 버젼: v18.17.1)
        ///
        /// - NPM package 설치
        ///
        ///   $ npm install xml-formatter
        let formatXml (xml:string): string =
            let cwd = __SOURCE_DIRECTORY__
            // Node.js 실행 파일과 스크립트 경로를 설정
            let nodePath = "node.exe"
            let scriptPath = Path.Combine(cwd, "formatXml.mjs")

            // 프로세스 시작 정보 설정
            let psi = ProcessStartInfo(nodePath, $"\"{scriptPath}\"")
            psi.WorkingDirectory <- cwd
            psi.RedirectStandardInput <- true
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false
            psi.StandardOutputEncoding <- Encoding.UTF8  // 출력 인코딩을 UTF-8로 설정
            psi.StandardErrorEncoding <- Encoding.UTF8   // 에러 인코딩을 UTF-8로 설정
            psi.StandardInputEncoding <- Encoding.UTF8   // 입력 인코딩을 UTF-8로 설정

            // 프로세스 시작
            use proc = Process.Start(psi)

            // XML 입력 전달
            proc.StandardInput.WriteLine(xml)
            proc.StandardInput.Close() // 입력 스트림 종료

            // 출력과 에러 읽기
            let output = proc.StandardOutput.ReadToEnd()
            let errors = proc.StandardError.ReadToEnd()

            // 프로세스 종료 대기
            proc.WaitForExit()

            // 에러가 있으면 예외 발생
            if errors.NonNullAny() then
                failwith $"ERROR while formatting xml: {errors}"

            // 포맷된 XML 반환
            output.Replace("\r\n", "\n").Replace("\n", "\r\n")



        /// 주어진 ds expression 코드를 파싱해서 PLC 코드 생성
        ///
        /// 부산물인 storages 와 statements 를 반환
        member x.TestCode (funcName:string) (code:string) =
            let storages = Storages()
            let statements = parseCodeForWindows storages code
            let xml = x.generateXmlForTest funcName storages (map withNoComment statements)
            x.saveTestResult funcName xml
            storages, statements

        [<SetUp>]
        member x.Setup () =
            RuntimeDS.System <- sys
            RuntimeDS.Package <- RuntimePackage.PLCSIM


        [<TearDown>]
        member __.TearDown () =
            RuntimeDS.System <- sys

        member __.saveTestResult testFunctionName (xml:string) = saveXgxTestResult xgx testFunctionName (formatXml xml)
        member __.generateXmlForTest = generateXmlForTest xgx
        member __.PlatformTarget = xgx
