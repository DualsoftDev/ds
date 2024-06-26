namespace Engine.Parser.FS

open System.IO
open Antlr4.Runtime.Tree

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open System

module ModelParser =
    let Walk (parser: dsParser, options: ParserOptions) =
        let listener = new DsParserListener(parser, options)
        let sysctx = parser.system ()
        ParseTreeWalker.Default.Walk(listener, sysctx)
        debugfn ("--- End of skeleton listener")
        parser.Reset()

        listener.CreateVertices(sysctx)

        for ctx in sysctx.Descendants<CausalPhraseContext>() do
            listener.ProcessCausalPhrase(ctx)

        for ctx in sysctx.Descendants<ButtonBlockContext>() do
            listener.ProcessButtonBlock(ctx)

        for ctx in sysctx.Descendants<LampBlockContext>() do
            listener.ProcessLampBlock(ctx)

        for ctx in sysctx.Descendants<ConditionBlockContext>() do
            listener.ProcessConditionBlock(ctx)

        for ctx in sysctx.Descendants<SafetyBlockContext>() do
            listener.ProcessSafetyBlock(ctx)



        listener

    let ParseFromString2 (text: string, options: ParserOptions) : DsParserListener =
        let (parser, _errors) = DsParser.FromDocument(text)
        let listener = Walk(parser, options)

        let system = listener.TheSystem
        system.CreateMRIEdgesTransitiveClosure()
        if system.ApiResetInfos.IsEmpty()
        then
            system.AutoAppendInterfaceReset()

        validateGraphOfSystem system
        validateRootCallConnection system

        listener


    let ParseFromString (text: string, options: ParserOptions) : DsSystem =
        ParseFromString2(text, options).TheSystem

    let Initialize () =
        debugfn "Initializing model parser"

        let loadSystemFromDsFile (param: DeviceLoadParameters) =
            let (dsFilePath, loadedName) = param.AbsoluteFilePath, param.LoadedName

          
            let dir = Path.GetDirectoryName(dsFilePath)

            let option =
                ParserOptions.Create4Runtime(
                    param.ShareableSystemRepository,
                    dir,
                    "ActiveCpuName",
                    Some param.AbsoluteFilePath,
                    param.LoadingType,
                    false
                )

            let option =
                { option with
                    LoadedSystemName = Option.ofObj loadedName }
            
            //경로 또는 시스템dsLib에 파일이 없으면 자동생성
            let sysDsLibPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), param.RelativeFilePath)

            match Path.Exists(dsFilePath), Path.Exists(sysDsLibPath) with
            | true, _ -> ParseFromString(File.ReadAllText(dsFilePath), option), false
            | false, true -> ParseFromString(File.ReadAllText(sysDsLibPath), option), false
            | false, false -> createDsSystem(loadedName), true


        let loadDevice (param: DeviceLoadParameters) =
            let device, autoGenFromParentSys = loadSystemFromDsFile param
            device.Name <- param.LoadedName
            Device(device, param, autoGenFromParentSys)

        let loadExternalSystem (param: DeviceLoadParameters) =
            let system, autoGenFromParentSys = loadSystemFromDsFile { param with LoadedName = null }
            ExternalSystem(system, param, autoGenFromParentSys)

        fwdLoadDevice <- loadDevice
        fwdLoadExternalSystem <- loadExternalSystem
        fwdParseFqdn <- FqdnParserModule.parseFqdn
