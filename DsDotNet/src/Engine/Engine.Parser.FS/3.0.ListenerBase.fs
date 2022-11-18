namespace Engine.Parser.FS

open Engine.Common.FS
open Engine.Core
open Engine.Parser
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser


/// <summary>
/// System, Flow, Task, Cpu
/// Parenting(껍데기만),
/// Segment Listing(root flow toplevel 만),
/// CallPrototype, Aliasing 구조까지 생성
/// </summary>
type ListenerBase(parser:dsParser, helper:ParserHelper) =
    inherit dsParserBaseListener()

    do
        parser.Reset()

    member x.ParserHelper = helper
    member internal _._theSystem = helper.TheSystem
    member internal _._flow       with get() = helper._flow      and set(v) = helper._flow      <- v
    member internal _._parenting  with get() = helper._parenting and set(v) = helper._parenting <- v




    override x.EnterSystem(ctx:SystemContext) =
        if helper.ParserOptions.LoadedSystemName.IsSome then
            DsParser.LoadedSystemName <- helper.ParserOptions.LoadedSystemName

    override x.ExitSystem(ctx:SystemContext) = DsParser.LoadedSystemName <- None

    override x.EnterFlowBlock(ctx:FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        let flow = helper.TheSystem.Value.Flows.TryFind(fun f -> f.Name = flowName)
        assert(flow.IsSome)
        x._flow <- flow

    override x.ExitFlowBlock(ctx:FlowBlockContext) = x._flow <- None



    override x.EnterParentingBlock(ctx:ParentingBlockContext) =
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        match x._flow with
        | Some flow ->
            let real = flow.Graph.Vertices.FindWithName(name) :?> Real
            x._parenting <- Some real
        | None -> failwith "ERROR"

    override x.ExitParentingBlock(ctx:ParentingBlockContext) = x._parenting <- None

