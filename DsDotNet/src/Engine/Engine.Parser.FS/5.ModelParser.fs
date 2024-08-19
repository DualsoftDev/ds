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


    let Walk (text: string, options: ParserOptions)  =
        let (parser, _errors) = DsParser.FromDocument(text)
        let listener = new DsParserListener(parser, options)
        let sysctx = parser.system()
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
        seq {
            let propsBlockCtxs = sysctx.Descendants<PropsBlockContext>().ToArray()
            for ctx in propsBlockCtxs do
                let actions = ctx.Descendants<MotionBlockContext>().ToList() |> ListnerCommonFunctionGeneratorUtil.getMotions
                yield! actions.Select(fun (fqdn, value) -> {Type = "Motion"; FQDN = fqdn; Value = value})

            for ctx in propsBlockCtxs do
                let scripts = ctx.Descendants<ScriptsBlockContext>().ToList() |> ListnerCommonFunctionGeneratorUtil.getScripts
                yield! scripts.Select(fun (fqdn, value) -> {Type = "Script"; FQDN = fqdn; Value = value})
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


    /// [job] block 내에 정의된 motion 및 script 에 대한 device api 추출?
    let WalkJobAddress (text: string, options: ParserOptions) =
        WalkAndExtract(text, options) |> snd |> ExtractJobBlock


    let private ParseFromString2 (text: string, options: ParserOptions) : DsParserListener =

        let listener = Walk(text, options)

        let system = listener.TheSystem
        createMRIEdgesTransitiveClosure4System system
        if system.ApiResetInfos.IsEmpty then
            autoAppendInterfaceReset system

        updateDeviceRootInfo system
        updateDeviceSkipAddress system

        validateGraphOfSystem system
        validateRootCallConnection system

        listener


    let _DicParsingSystem = Dictionary<string, DsSystem>() //동일 절대경로는 기존 dsParser를 재사용하기 위함
    let ClearDicParsingText() = _DicParsingSystem.Clear()
    let private getAbsolutePath(options: ParserOptions) =
        if options.AbsoluteFilePath.IsSome then
           options.AbsoluteFilePath.Value  else   ""

    let ParseFromString (text: string, options: ParserOptions) : DsSystem =

        if options.IsNewModel then
            ClearDicParsingText()

        let path = getAbsolutePath options

        let newParsing skipAddDict  =
            let sys = ParseFromString2(text, options).TheSystem

            if sys.Jobs.IsEmpty && not(skipAddDict) then //하위 디바이스가 없어야 system Clone 등록 가능
                _DicParsingSystem.Add(path, sys) |> ignore
            sys

        if _DicParsingSystem.ContainsKey(path) then
            match options.LoadedSystemName with
            | Some loadedName ->
                let cloneSys = _DicParsingSystem[path].Clone(loadedName)
                cloneSys
            | None -> newParsing true

        else
            newParsing false


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
