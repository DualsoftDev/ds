namespace Engine.Parser.FS

open Engine.Core
open System.Linq
open type Engine.Parser.dsParser
open type DsParser

open Antlr4.Runtime.Tree

open Engine.Common.FS

[<AutoOpen>]
module ParserRuleContextModule =

        let createCallDef (system:DsSystem) (ctx:CallListingContext) =
            let callName =  tryFindFirstChild<CallNameContext>(ctx).Map(getText).Value
            let apiDefCtxs = enumerateChildren<CallApiDefContext>(ctx).ToArray()
            let getAddress (addressCtx:IParseTree) =
                tryFindFirstChild<AddressItemContext>(addressCtx).Map(getText).Value
            let apiItems =
                [   for apiDefCtx in apiDefCtxs do
                    let apiPath = collectNameComponents apiDefCtx |> List.ofSeq // e.g ["A"; "+"]
                    match apiPath with
                    | device::api::[] ->
                        let apiItem =
                            option {
                                let! apiPoint = tryFindCallingApiItem system device api
                                let! addressCtx = tryFindFirstChild<AddressTxRxContext>(ctx)
                                let! txAddressCtx = tryFindFirstChild<TxContext>(addressCtx)
                                let! rxAddressCtx = tryFindFirstChild<RxContext>(addressCtx)
                                let tx = getAddress(txAddressCtx)
                                let rx = getAddress(rxAddressCtx)

                                tracefn $"TX={tx} RX={rx}"
                                return ApiItem(apiPoint, tx, rx)
                            }
                        match apiItem with
                        | Some apiItem -> yield apiItem
                        | _ -> failwith "ERROR"

                    | _ -> failwith "ERROR"
                ]
            assert(apiItems.Any())
            Call(callName, apiItems) |> system.Calls.Add


        let createAliasDef (system:DsSystem) (ctx:AliasListingContext) =
            let ci = getContextInformation ctx
            let flow = tryFindFlow system ci.Flow.Value |> Option.get
            let map = flow.AliasDefs
            let mnemonics = enumerateChildren<AliasMnemonicContext>(ctx).Select(getText).ToArray()
            let aliasTargetCtx = tryFindFirstChild<AliasDefContext>(ctx).Value    // {타시스템}.{interface} or {Flow}.{real}
            let aliasTargetNames = collectNameComponents(aliasTargetCtx).ToFSharpList()
            let target =
                match aliasTargetNames with
                | t::[] ->
                    let realTargetCandidate = flow.Graph.TryFindVertex<Real>(t)
                    let callTargetCandidate = tryFindCall system t
                    match realTargetCandidate, callTargetCandidate with
                    | Some real, None -> RealTarget real
                    | None, Some call -> CallTarget call
                    | Some _, Some _ -> failwith "Name duplicated."
                    | _ -> failwith "Failed to find"
                | otherFlow::real::[] ->
                    match tryFindReal system otherFlow real with
                    | Some otherFlowReal -> RealTarget otherFlowReal
                    | _ -> failwith "Failed to find"
                | _ ->
                    failwith "ERROR"

            { AliasTarget=target; Mnemonincs=mnemonics} |> map.Add

        let createCallDefs (helper:ParserHelper) = helper._callListingContexts.Iter(createCallDef helper.TheSystem.Value)
        let createAliasDefs (helper:ParserHelper) = helper._aliasListingContexts.Iter(createAliasDef helper.TheSystem.Value)
