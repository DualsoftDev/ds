namespace rec Engine.Parser.FS

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open type Engine.Parser.dsParser
open Engine.Parser
open System.Collections.Generic
open Antlr4.Runtime.Tree
open System.Text.RegularExpressions
open Antlr4.Runtime

[<AutoOpen>]
module ListnerCommonFunctionGeneratorUtil =

    type RuleContext with
        member x.Error(?hint:string) =
            let hint = hint |? ""
            let posi, ambi = ParserError.CreatePositionInfo(x)
            failwithlog ($"[규칙확인]: {hint}\r\n{posi} near '{ambi}'")

    // Helper function to find Real or Call
    let getSafetyAutoPreCall (curSystem: DsSystem) (ns: Fqdn) =
        let findFlowOrFail f =
            match curSystem.TryFindFlow(f) with
            | Some(flow) -> flow
            | None -> failwithlog "ERROR"

        match ns.ToFSharpList() with
        | f :: [call; api] ->
            findFlowOrFail f
            |> fun flow -> flow.GetVerticesHasJobOfFlow()
                               .First(fun v -> v.Name = $"{call}.{api}")

        | f :: [real; call; api] ->
            findFlowOrFail f
            |> fun flow ->
                flow.GetVerticesOfFlow().OfType<Call>()
                    .Where(fun c -> c.Parent.GetCore().Name = real)
                    .First(fun c -> c.Name = $"{call}.{api}")

        | f :: [real; otherFlow; call; api] ->
            findFlowOrFail f
            |> fun flow ->
                flow.GetVerticesOfFlow().OfType<Call>()
                    .Where(fun c -> c.Parent.GetCore().Name = real)
                    .First(fun c -> c.Name = $"{otherFlow}.{call}.{api}")

        | _ -> failwithlog "ERROR"


    let getSafetyAutoPreDefs  (ctx: dsParser.SafetyAutoPreDefContext seq) =
            (*
             * safety block 을 parsing 해서 key / value 의 dictionary 로 저장
             *
            [safety] = {
                F.Main = {A."+"; B."+"}
            }
            => "Main" = {A."+"; B."+"}
             *)
            let safetyKvs =
                [
                    for safetyDef in ctx do
                        let key =
                            let safety =
                                safetyDef.TryFindFirstChild(fun (t: IParseTree) -> t :? SafetyAutoPreKeyContext).Value

                            safety.CollectNameComponents() // ["Main"] or ["My", "Flow", "Main"]

                        let valueHeader = safetyDef.Descendants<SafetyAutoPreValuesContext>().First()

                        let values =
                            valueHeader
                                .Descendants<Identifier45Context>()
                                .Select(collectNameComponents)
                                .ToArray()

                        (key, values)
                ]

            safetyKvs

    type DevApiDefinition = {
            ApiFqnd : string array
            TaskDevParamIO : TaskDevParamIO
        }

    type TimeDefinition = {
        Average: CountUnitType option
        Std: CountUnitType option
    }

    let getTimes (listTimeCtx: List<dsParser.TimesBlockContext>) : seq<string list * TimeDefinition> =
        let parseTimeParams (name, timeParams: string) : TimeDefinition =
            try
                let TimeDefinition = {
                    Average       = parseUIntMSec timeParams TextAVG
                    Std           = parseUIntMSec timeParams TextSTD
                }
                TimeDefinition
            with ex
                -> failWithLog $"{name} {timeParams} {ex.Message}"


        seq {
            for ctx in listTimeCtx do
                let list = ctx.Descendants<TimeDefContext>().ToList()
                for defs in list do
                    let v = defs.TryFindFirstChild<TimeKeyContext>() |> Option.get
                    let fqdn = collectNameComponents v |> List.ofArray
                    let path = defs.TryFindFirstChild<TimeParamsContext>() |> Option.get
                    let timeDef = parseTimeParams (fqdn.CombineQuoteOnDemand(), path.GetText())
                    yield fqdn, timeDef
        }

    type ErrorDefinition = {
        TimeOutMaxTime: CountUnitType option
        CheckDelayTime: CountUnitType option
    }

    let getErrors (listErrorCtx: List<dsParser.ErrorsBlockContext>) : seq<string list * ErrorDefinition> =
        let parseErrorParams (name, errorParams: string) : ErrorDefinition =
            // ErrorDefinition 생성하여 반환
            try
                let errorDefinition = {
                    TimeOutMaxTime = parseUIntMSec errorParams TextMAX
                    CheckDelayTime = parseUIntMSec errorParams TextCHK
                }
                errorDefinition
            with ex
                -> failWithLog $"{name} {errorParams} {ex.Message}"

        seq {
            for ctx in listErrorCtx do
                let list = ctx.Descendants<ErrorsDefContext>().ToList()
                for defs in list do
                    let v = defs.TryFindFirstChild<ErrorsKeyContext>() |> Option.get
                    let fqdn = collectNameComponents v |> List.ofArray
                    let path = defs.TryFindFirstChild<ErrorsParamsContext>() |> Option.get
                    let ErrorDef = parseErrorParams (fqdn.CombineQuoteOnDemand(), path.GetText())
                    yield fqdn, ErrorDef
        }



    let getRepeats  (listRepeatCtx: List<dsParser.RepeatsBlockContext>) =
                seq {
                    for ctx in listRepeatCtx do
                    let list = ctx.Descendants<RepeatDefContext>().ToList()
                    for defs in list do
                        let v = defs.TryFindFirstChild<RepeatKeyContext>() |> Option.get
                        let fqdn = collectNameComponents v |> List.ofArray
                        let path = defs.TryFindFirstChild<RepeatParamsContext>() |> Option.get
                        yield fqdn, path.GetText()
                }
    let getMotions  (listMotionCtx: List<dsParser.MotionBlockContext>) =
                seq {
                    for ctx in listMotionCtx do
                    let list = ctx.Descendants<MotionDefContext>().ToList()
                    for defs in list do
                        let v = defs.TryFindFirstChild<MotionKeyContext>() |> Option.get
                        let fqdn = collectNameComponents v |> List.ofArray
                        let path = defs.TryFindFirstChild<MotionParamsContext>() |> Option.get
                        yield fqdn, path.GetText()
                }
    let getScripts (listScriptsCtx: List<dsParser.ScriptsBlockContext>) =
                seq {
                    for ctx in listScriptsCtx do
                    let list = ctx.Descendants<ScriptDefContext>().ToList()
                    for defs in list do
                        let v = defs.TryFindFirstChild<ScriptKeyContext>() |> Option.get
                        let fqdn = collectNameComponents v |> List.ofArray
                        let script = defs.TryFindFirstChild<ScriptParamsContext>() |> Option.get
                        yield fqdn, script.GetText()
                }



    let getCode (executeCode:String) =
        assert( (executeCode.StartsWith("${") || executeCode.StartsWith("#{")) && executeCode.EndsWith("}"))
        // 처음 "#{" or "${"와 끝의  "}" 제외
        let pureCode = executeCode.Substring(2, executeCode.Length - 2).TrimEnd('}')
        pureCode.Split(';')
            .Map(fun s->s.Trim().Trim([|'\r';'\n'|]))
            .JoinWith(";\r\n").Trim([|'\r';'\n'|])

    let commonFunctionCommandExtractor (fDef: CommandDefContext)=
        // 함수 호출과 관련된 매개변수 추출
        let executeCode = fDef.command().GetText()
        executeCode |> getCode

    let commonFunctionOperatorExtractor (fDef: OperatorDefContext)=
        // 함수 호출과 관련된 매개변수 추출
        let executeCode = fDef.operator().GetText()
        executeCode |> getCode

    let commonDeviceParamExtractor (devCtx: TaskDevParamInOutContext) : (TaskDevParam)*(TaskDevParam) =
        match devCtx.TryFindFirstChild<TaskDevParamInOutBodyContext>() with
        | Some ctx ->
            match tryGetTaskDevParamInOut $"{ctx.GetText()}" with
            | Some v -> v
            |_ -> ctx.Error()
        | _-> devCtx.Error()

    let commonValueParamExtractor (devCtx: TaskDevParamInOutContext) : (string)*(string) =
        match devCtx.TryFindFirstChild<TaskDevParamInOutBodyContext>() with
        | Some ctx ->
            match tryGetHwSysValueParamInOut $"{ctx.GetText()}" with
            | Some v -> v
            |_ -> ctx.Error()
        | _-> devCtx.Error()


    let commonCallParamExtractor (ctx: JobBlockContext) =
        let callListings = ctx.Descendants<CallListingContext>().ToArray()
        [
            for callListingCtx in callListings do
                let item = callListingCtx.TryFindFirstChild<JobNameContext>().Value.GetText()

                let jobFqdn = item.Split('.').Select(fun s->s.DeQuoteOnDemand()).ToArray()

                let apiDefCtxs = callListingCtx.Descendants<CallApiDefContext>().ToArray()
                yield jobFqdn,  apiDefCtxs, callListingCtx
        ]
    let createApiResetInfo (terms:string array) (sys:DsSystem) =
        if terms.Contains("|>") || terms.Contains("<|") then
            // I1 |> I2 <| I3 <|> I4 에 대해서 해석
            let processTerms (terms: string[]) =
                let mutable currentTerms = []
                let mutable edges = []
                let mutable lastOperator = ""

                for term in terms do
                    match term with
                    | "|>" | "<|" | "<|>" ->
                        if currentTerms.Length > 1 then
                            let opnd1 = currentTerms.[currentTerms.Length - 2]
                            let opnd2 = currentTerms.[currentTerms.Length - 1]
                            edges <- (opnd1, lastOperator, opnd2) :: edges
                        lastOperator <- term
                    | _ ->
                        currentTerms <- currentTerms @ [term]

                // Add the final edge
                if currentTerms.Length > 1 then
                    let opnd1 = currentTerms.[currentTerms.Length - 2]
                    let opnd2 = currentTerms.[currentTerms.Length - 1]
                    edges <- (opnd1, lastOperator, opnd2) :: edges

                edges.Reverse()

            let edgesToCreate = processTerms terms

            // Create edges
            for (opnd1, op, opnd2) in edgesToCreate do
                sys.CreateApiResetInfo(opnd1, op |> toModelEdge, opnd2, false) |> ignore

        else
            // I1 <|> I2 와 I2 <|> I3 에 대해서 해석
            let apis = terms |> Array.filter (fun f -> f <> "<|>")
            let resets =
                apis
                |> Seq.allPairs apis
                |> Seq.filter (fun (l, r) -> l <> r)
                |> Seq.distinctBy (fun (l, r) -> [| l; r |] |> Array.sort |> String.concat ";")

            for (left, right) in resets do
                let opnd1, op, opnd2 = left, "<|>", right
                sys.CreateApiResetInfo(opnd1, op |> toModelEdge, opnd2, false) |> ignore
