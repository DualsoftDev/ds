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
            option {
                let! flow = tryFindFlow system ci.Flow.Value
                let! aliasKeys = tryFindFirstChild<AliasDefContext>(ctx).Map(collectNameComponents)
                let mnemonics = enumerateChildren<AliasMnemonicContext>(ctx).Select(getText).ToArray()
                let ad = AliasDef(aliasKeys, None, mnemonics)
                flow.AliasDefs.Add(aliasKeys, ad)
                return ad
            }

        let fillTargetOfAliasDef (system:DsSystem) (ctx:AliasListingContext) =
            let ci = getContextInformation ctx
            option {
                let! flow = tryFindFlow system ci.Flow.Value
                let mnemonics = enumerateChildren<AliasMnemonicContext>(ctx).Select(getText).ToArray()
                let! aliasKeys = tryFindFirstChild<AliasDefContext>(ctx).Map(collectNameComponents)
                let target =
                    match aliasKeys.ToFSharpList() with
                    | t::[] ->
                        let realTargetCandidate = flow.Graph.TryFindVertex<Real>(t)
                        let callTargetCandidate = tryFindCall system t
                        match realTargetCandidate, callTargetCandidate with
                        | Some real, None -> Some (RealTarget real)
                        | None, Some call -> Some (CallTarget call)
                        | Some _, Some _ -> failwith "Name duplicated."
                        | _ -> failwith "Failed to find"
                    | otherFlow::real::[] ->
                        match tryFindReal system otherFlow real with
                        | Some otherFlowReal -> Some (RealTarget otherFlowReal)
                        | _ -> failwith "Failed to find"
                    | _ ->
                        failwith "ERROR"

                flow.AliasDefs[aliasKeys].AliasTarget <- target
            }


        let createCallDefs (helper:ParserHelper) =
            helper._callListingContexts.Iter(createCallDef helper.TheSystem.Value)
        let createAliasDefs (helper:ParserHelper) =
            helper._aliasListingContexts.Iter(fun ctx ->
                createAliasDef helper.TheSystem.Value ctx |> ignore)
        let fillAliasDefsTarget (helper:ParserHelper) =
            helper._aliasListingContexts.Iter(fun ctx ->
                fillTargetOfAliasDef helper.TheSystem.Value ctx |> ignore)
        let fillInterfaceDefs (helper:ParserHelper) =
            ()
            //helper._interfaceDefContexts.Iter(fun ctx ->
            //    let system = helper.TheSystem.Value
            //    system.ApiItems4Export
            //    fillTargetOfAliasDef helper.TheSystem.Value ctx |> ignore)
