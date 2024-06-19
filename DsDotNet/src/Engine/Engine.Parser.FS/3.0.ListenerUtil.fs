namespace rec Engine.Parser.FS

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open type Engine.Parser.dsParser


[<AutoOpen>]
module ListnerCommonFunctionGeneratorUtil =

    let commonOpFunctionExtractor (funcCallCtxs: FuncCallContext array) (callName:string) (system:DsSystem) =
        if funcCallCtxs.Length > 1 
        then 
            failwithlog $"not support job multi function {callName}"

        if funcCallCtxs.any() 
            then 
                let funcName = funcCallCtxs.Head().GetText().TrimStart('$')
                Some (system.Functions.Cast<OperatorFunction>().First(fun f->f.Name = funcName))
            else None 

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

    let commonDeviceParamExtractor (devCtx: DevParamInOutContext) : DevParam * DevParam =
        devCtx.TryFindFirstChild<DevParamInOutBodyContext>()
        |> Option.map (fun ctx -> getDevParamInOut $"{ctx.GetText()}")
        |> Option.defaultWith(fun () -> failWithLog "commonDeviceParamExtractor error")

    
    let commonCallParamExtractor (ctx: JobBlockContext) =
        let callListings = ctx.Descendants<CallListingContext>().ToArray()
        [
            for callList in callListings do
                let jobName = callList.TryFindFirstChild<JobNameContext>().Value.GetText().DeQuoteOnDemand()     
                let jobOption =
                    callList.TryFindFirstChild<JobTypeOptionContext>()
                    |> Option.map (fun ctx -> ctx.GetText().DeQuoteOnDemand())

                let apiDefCtxs = callList.Descendants<CallApiDefContext>().ToArray()
                yield jobName, jobOption, apiDefCtxs
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
                ApiResetInfo.Create(sys, opnd1, op |> toModelEdge, opnd2, false) |> ignore

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
                ApiResetInfo.Create(sys, opnd1, op |> toModelEdge, opnd2, false) |> ignore
