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

        let fillInterfaceDef (system:DsSystem) (ctx:InterfaceDefContext) =
            let findSegments(fqdns:Fqdn[]):Real[] =
                fqdns
                    .Where(fun fqdn -> fqdn <> null)
                    .Select(fun s -> system.FindGraphVertex<Real>(s))
                    .Tap(fun x -> assert(not (isItNull x)))
                    .ToArray()
            let isWildcard(cc:Fqdn):bool = cc.Length = 1 && cc[0] = "_"
            let collectCallComponents(ctx:CallComponentsContext):Fqdn[] =
                enumerateChildren<Identifier123Context>(ctx)
                    .Select(collectNameComponents)
                    .ToArray()
            option {
                let! interrfaceNameCtx = tryFindFirstChild<InterfaceNameContext>(ctx)
                let interfaceName = collectNameComponents(interrfaceNameCtx)[0]
                let! api = system.ApiItems4Export.TryFind(nameEq interfaceName)
                let ser =   // { start ~ end ~ reset }
                    enumerateChildren<CallComponentsContext>(ctx)
                        .Map(collectCallComponents)
                        .Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || isWildcard(cc))))
                        .Select(fun callCompnents -> callCompnents.Select(fun cc -> if isWildcard(cc) then null else cc.Prepend(system.Name).ToArray()).ToArray())
                        .ToArray()

                let n = ser.Length

                assert(n = 2 || n = 3)
                api.AddTXs(findSegments(ser[0])) |> ignore
                api.AddRXs(findSegments(ser[1])) |> ignore
            } |> ignore


        let createCallDefs (helper:ParserHelper) =
            helper._callListingContexts.Iter(createCallDef helper.TheSystem)
        let createAliasDefs (helper:ParserHelper) =
            helper._aliasListingContexts.Iter(fun ctx ->
                createAliasDef helper.TheSystem ctx |> ignore)
        let fillAliasDefsTarget (helper:ParserHelper) =
            helper._aliasListingContexts.Iter(fun ctx ->
                fillTargetOfAliasDef helper.TheSystem ctx |> ignore)

        let fillInterfaceDefs (helper:ParserHelper) =
            helper._interfaceDefContexts.Iter(fun ctx ->
                fillInterfaceDef helper.TheSystem ctx |> ignore)

