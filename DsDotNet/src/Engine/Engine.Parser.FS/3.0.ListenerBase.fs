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

    override x.EnterSystem(ctx:SystemContext) =
        if helper.ParserOptions.LoadedSystemName.IsSome then
            DsParser.LoadedSystemName <- helper.ParserOptions.LoadedSystemName

    override x.ExitSystem(ctx:SystemContext) = DsParser.LoadedSystemName <- None

    override x.EnterFlowBlock(ctx:FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        let flow = helper.TheSystem.Flows.TryFind(fun f -> f.Name = flowName).Value
        helper._flow <- flow

    override x.ExitFlowBlock(ctx:FlowBlockContext) =
        helper._flow <- getNull<Flow>()



    override x.EnterParentingBlock(ctx:ParentingBlockContext) =
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        if isItNull helper._flow then
            failwith "ERROR"
        else
            let real = helper._flow.Graph.Vertices.FindWithName(name) :?> Real
            helper._parenting <- real

    override x.ExitParentingBlock(ctx:ParentingBlockContext) =
        helper._parenting <- getNull<Real>()

