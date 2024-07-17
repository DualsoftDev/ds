namespace Engine.Parser.FS

open System.IO
open Antlr4.Runtime.Tree

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open System
open System.Linq
open Antlr4.Runtime
open System.Collections.Generic


module ModelParser =


    let WalkAndExtract (text: string, options: ParserOptions) =
        let (parser, _errors) = DsParser.FromDocument(text)
        let _ = new DsParserListener(parser, options)
        parser, parser.system()


    let Walk (parser: dsParser, sysctx:SystemContext, options: ParserOptions) =
        let listener = new DsParserListener(parser, options)
        ParseTreeWalker.Default.Walk(listener, sysctx)
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

        for ctx in sysctx.Descendants<AutoPreBlockContext>() do
            listener.ProcessAutoPreBlock(ctx)



        listener

    type DsProperty = {
        Type: string
        FQDN: string list
        Value: string
    }


    let ExtractPropsBlock (sysctx: ParserRuleContext) =
        seq{
            for ctx in sysctx.Descendants<PropsBlockContext>() do
                let actions = ctx.Descendants<MotionBlockContext>().ToList() |> ListnerCommonFunctionGeneratorUtil.getMotions 
                yield! actions.Select(fun (fqdn, value)-> {Type = "Motion"; FQDN = fqdn; Value = value})

            for ctx in sysctx.Descendants<PropsBlockContext>() do
                let scripts = ctx.Descendants<ScriptsBlockContext>().ToList() |> ListnerCommonFunctionGeneratorUtil.getScripts 
                yield! scripts.Select(fun (fqdn, value)-> {Type = "Script"; FQDN = fqdn; Value = value})
        }

    let ExtractJobBlock  (sysctx: ParserRuleContext) =
        [
            for ctx in sysctx.Descendants<JobBlockContext>() do
                let callListings = commonCallParamExtractor ctx 
                for jobNameFqdn, _jobParam, _apiDefCtxs, callListingCtx in callListings do
                    yield getAutoGenDevApi (jobNameFqdn,  callListingCtx) 
        ]

    let WalkProperty (text: string, options: ParserOptions) =
        WalkAndExtract(text, options) |> snd |> ExtractPropsBlock


    let WalkJobAddress (text: string, options: ParserOptions) =
        WalkAndExtract(text, options) |> snd |> ExtractJobBlock
    
    let _DicParsingText = Dictionary<string, dsParser*SystemContext>() //동일 절대경로는 기존 dsParser를 재사용하기 위함
    let ClearDicParsingText() = _DicParsingText.Clear()

    let ParseFromString2 (text: string, options: ParserOptions) : DsParserListener =
        if options.IsNewModel then
            ClearDicParsingText()

        let (parser, sysCtx) =
            match options.AbsoluteFilePath with
            | Some path when _DicParsingText.ContainsKey(path) -> 
                _DicParsingText[path]
            | _ -> 
                let parserNctx =  WalkAndExtract (text,  options)
                let path = if options.AbsoluteFilePath.IsSome then 
                              options.AbsoluteFilePath.Value
                            else 
                                "" 

                _DicParsingText.Add(path, parserNctx)
                _DicParsingText[path]
        

        let listener = Walk(parser, sysCtx, options)

        let system = listener.TheSystem
        createMRIEdgesTransitiveClosure4System system
        if system.ApiResetInfos.IsEmpty() then
            autoAppendInterfaceReset system

        updateDeviceRootInfo system
        updateDeviceSkipAddress system     

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
                    false,
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
