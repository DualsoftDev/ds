namespace Engine.Parser.FS

open Engine.Common.FS
open Engine.Core
open Engine.Parser
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open System.Collections.Generic
open Antlr4.Runtime


/// <summary>
/// System, Flow, Task, Cpu
/// Parenting(껍데기만),
/// Segment Listing(root flow toplevel 만),
/// CallPrototype, Aliasing 구조까지 생성
/// </summary>
type ListenerBase(parser:dsParser, options:ParserOptions) =
    inherit dsParserBaseListener()

    do
        parser.Reset()

    member val YYLoadedSystemName:string option = None with get, set
    member val ParserOptions = options with get, set

    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    member val TheSystem:DsSystem = getNull<DsSystem>() with get, set

    member val internal _flow = getNull<Flow>() with get, set
    member val internal _parenting = getNull<Real>()  with get, set


    member val internal _aliasListingContexts        = ResizeArray<AliasListingContext>()
    member val internal _callListingContexts         = ResizeArray<CallListingContext>()
    member val internal _parentingBlockContexts      = ResizeArray<ParentingBlockContext>()

    member val internal _causalPhraseContexts        = ResizeArray<CausalPhraseContext>()
    member val internal _causalTokenContext          = ResizeArray<CausalTokenContext>()
    member val internal _identifier12ListingContexts = ResizeArray<Identifier12ListingContext>()


    member val internal _interfaceDefContexts = ResizeArray<InterfaceDefContext>()



    member val internal _deviceBlockContexts         = ResizeArray<LoadDeviceBlockContext>()
    member val internal _externalSystemBlockContexts = ResizeArray<LoadExternalSystemBlockContext>()

    member val internal RuleDictionary = Dictionary<ParserRuleContext, string>()

    override x.EnterEveryRule(ctx:ParserRuleContext) =
        match x.YYLoadedSystemName with
        | Some systemName -> x.RuleDictionary.Add(ctx, systemName)
        | None -> ()


    override x.EnterSystem(ctx:SystemContext) =
        match options.LoadedSystemName with
        | Some systemName ->
                x.YYLoadedSystemName <- Some systemName
                x.RuleDictionary.Add(ctx, systemName)
        | _ -> ()

    override x.ExitSystem(ctx:SystemContext) = x.YYLoadedSystemName <- None

    override x.EnterFlowBlock(ctx:FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        let flow = x.TheSystem.Flows.TryFind(fun f -> f.Name = flowName).Value
        x._flow <- flow

    override x.ExitFlowBlock(ctx:FlowBlockContext) =
        x._flow <- getNull<Flow>()



    override x.EnterParentingBlock(ctx:ParentingBlockContext) =
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        if isItNull x._flow then
            failwith "ERROR"
        else
            let real = x._flow.Graph.Vertices.FindWithName(name) :?> Real
            x._parenting <- real

    override x.ExitParentingBlock(ctx:ParentingBlockContext) =
        x._parenting <- getNull<Real>()

