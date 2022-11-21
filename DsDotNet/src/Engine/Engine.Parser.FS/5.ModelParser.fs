namespace Engine.Parser.FS

open System.IO
open Antlr4.Runtime.Tree

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser

module ModelParser =
    let mutable initialized = false
    let Walk(parser:dsParser, options:ParserOptions) =
        let listener = new SkeletonListener(parser, options)
        let sysctx = parser.system()
        ParseTreeWalker.Default.Walk(listener, sysctx)
        tracefn("--- End of skeleton listener")
        parser.Reset()

        listener.CreateVertices(sysctx)

        for ctx in sysctx.Descendants<CausalPhraseContext>() do
            listener.ProcessCausalPhrase(ctx)

        for ctx in sysctx.Descendants<ButtonsBlocksContext>() do
            listener.ProcessButtonsBlocks(ctx)

        for ctx in sysctx.Descendants<SafetyBlockContext>() do
            listener.ProcessSafetyBlock(ctx)

        for ctx in sysctx.Descendants<VariableDefContext>() do
            listener.ProcessVariableDef(ctx)

        for ctx in sysctx.Descendants<CommandDefContext>() do
            listener.ProcessCommandDef(ctx)

        for ctx in sysctx.Descendants<ObserveDefContext>() do
            listener.ProcessObserveDef(ctx)

        listener.ProcessLayouts(sysctx)

        listener

    let ParseFromString2(text:string, options:ParserOptions):SkeletonListener =
        assert(initialized)
        let (parser, errors) = DsParser.FromDocument(text)
        let listener = Walk(parser, options)

        let system = listener.TheSystem
        system.CreateMRIEdgesTransitiveClosure()

        system.Validate() |> ignore

        listener


    let ParseFromString(text:string, options:ParserOptions):DsSystem = ParseFromString2(text, options).TheSystem

    let Initialize() =
        tracefn "Initializing"
        initialized <- true
        let loadSystemFromDsFile (dsFilePath, loadedName) =
            let text = File.ReadAllText(dsFilePath)
            let dir = Path.GetDirectoryName(dsFilePath)
            let option = ParserOptions.Create4Runtime(dir, "ActiveCpuName")
            option.LoadedSystemName <- Some loadedName
            let system = ParseFromString(text, option)
            system

        let loadDevice (constainerSystem:DsSystem) (absoluteFilePath, simpleFilePath) loadedName =
            let system = loadSystemFromDsFile (absoluteFilePath, loadedName)
            system.Name <- loadedName
            Device(system, constainerSystem, (absoluteFilePath, simpleFilePath))

        let loadExternalSystem (constainerSystem:DsSystem) (absoluteFilePath, simpleFilePath) loadedName =
            let system = loadSystemFromDsFile (absoluteFilePath, loadedName)
            ExternalSystem(loadedName, system, constainerSystem, (absoluteFilePath, simpleFilePath))

        fwdLoadDevice <- loadDevice
        fwdLoadExternalSystem <- loadExternalSystem
