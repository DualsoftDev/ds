namespace Engine.Parser.FS

open System.IO
open Antlr4.Runtime.Tree

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser

module ModelParser =
    let Walk (parser: dsParser, options: ParserOptions) =
        let listener = new DsParserListener(parser, options)
        let sysctx = parser.system ()
        ParseTreeWalker.Default.Walk(listener, sysctx)
        tracefn ("--- End of skeleton listener")
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

        //DsSystem.OriginalCodeBlocks 여기에 저장 및 불러오기로 이동
        for ctx in sysctx.Descendants<VariableDefContext>() do
            listener.ProcessVariableDef(ctx)

        //for ctx in sysctx.Descendants<CommandDefContext>() do
        //    listener.ProcessCommandDef(ctx)

        //for ctx in sysctx.Descendants<ObserveDefContext>() do
        //    listener.ProcessObserveDef(ctx)

        //listener.ProcessLayouts(sysctx)

        listener

    let ParseFromString2 (text: string, options: ParserOptions) : DsParserListener =
        let (parser, _errors) = DsParser.FromDocument(text)
        let listener = Walk(parser, options)

        let system = listener.TheSystem
        system.CreateMRIEdgesTransitiveClosure()

        system.ValidateGraph() |> ignore

        listener


    let ParseFromString (text: string, options: ParserOptions) : DsSystem =
        ParseFromString2(text, options).TheSystem

    let Initialize () =
        tracefn "Initializing model parser"

        let loadSystemFromDsFile (param: DeviceLoadParameters) =
            let (dsFilePath, loadedName) = param.AbsoluteFilePath, param.LoadedName
            let text = File.ReadAllText(dsFilePath)
            let dir = Path.GetDirectoryName(dsFilePath)

            let option =
                ParserOptions.Create4Runtime(
                    param.ShareableSystemRepository,
                    dir,
                    "ActiveCpuName",
                    Some param.AbsoluteFilePath,
                    param.LoadingType
                )

            let option =
                { option with
                    LoadedSystemName = Option.ofObj loadedName }

            let system = ParseFromString(text, option)
            system

        let loadDevice (param: DeviceLoadParameters) =
            let device = loadSystemFromDsFile param
            device.Name <- param.LoadedName
            Device(device, param)

        let loadExternalSystem (param: DeviceLoadParameters) =
            let system = loadSystemFromDsFile { param with LoadedName = null }
            ExternalSystem(system, param)

        fwdLoadDevice <- loadDevice
        fwdLoadExternalSystem <- loadExternalSystem
        fwdParseFqdn <- FqdnParser.parseFqdn
