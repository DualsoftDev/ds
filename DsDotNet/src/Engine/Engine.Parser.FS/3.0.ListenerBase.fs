namespace Engine.Parser.FS

open System.Linq
open Engine.Common.FS
open Engine.Core
open Engine.Parser
open type Engine.Parser.dsParser


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
    member internal _._model = helper.Model
    member internal _._elements = helper._elements
    member internal _._currentSystem = helper._currentSystem
    member internal _._theSystem = helper._theSystem
    member internal _._modelSpits = helper._modelSpits
    member internal _._modelSpitObjects = helper._modelSpitObjects
    member internal _._flow       with get() = helper._flow      and set(v) = helper._flow      <- v
    member internal _._parenting  with get() = helper._parenting and set(v) = helper._parenting <- v

    member internal x.AddElement(path:Fqdn, elementType:GraphVertexType) =
        if x._elements.ContainsKey(path) then
            x._elements[path] <- (x._elements[path] ||| elementType)
        else
            x._elements.Add(path, elementType)

    member internal _.AppendPathElement(name:string) = helper.AppendPathElement(name)
    member internal _.AppendPathElement(names:Fqdn)  = helper.AppendPathElement(names)
    member internal _.CurrentPathElements = helper.CurrentPathElements
    member internal x.UpdateModelSpits() = helper.UpdateModelSpits()



    override x.EnterModel(ctx:ModelContext) = x.UpdateModelSpits()

    override x.EnterSystem(ctx:SystemContext) =
        let name = ctx.systemName().GetText().DeQuoteOnDemand()
        match x._currentSystem with
        | None ->
            assert(name = helper._theSystem.Value.Name)
            helper._currentSystem <- helper._theSystem
        | Some curSys ->
            helper._currentSystem <- Some <| curSys.Systems.Find(fun s -> s.Name = name)

    override x.ExitSystem(ctx:SystemContext) = helper._currentSystem <- None

    override x.EnterFlow(ctx:FlowContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        let flow = x._currentSystem.Value.Flows.TryFind(fun f -> f.Name = flowName)
        assert(flow.IsSome)
        x._flow <- flow

    override x.ExitFlow(ctx:FlowContext) = x._flow <- None



    override x.EnterParenting(ctx:ParentingContext) =
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        match x._flow with
        | Some flow ->
            let real = flow.Graph.Vertices.FindWithName(name) :?> Real
            x._parenting <- Some real
        | None -> failwith "ERROR"

    override x.ExitParenting(ctx:ParentingContext) = x._parenting <- None

