namespace Engine.Parser.FS

open System.Linq
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
    member internal _._modelSpits = helper._modelSpits
    member internal _._modelSpitObjects = helper._modelSpitObjects
    member internal _._flow       with get() = helper._flow      and set(v) = helper._flow      <- v
    member internal _._parenting  with get() = helper._parenting and set(v) = helper._parenting <- v


    member internal x.AddElement(contextInformation:ContextInformation, elementType:GVT) =
        let ci = contextInformation
        let es = helper._elements

        if es.ContainsKey(ci) then
            failwith "ERROR: duplicated"
            es[ci] <- (es[ci] ||| elementType)
        else
            es.Add(ci, elementType)
        //logDebug $"Added Element: {ci}={es[ci]}"

    member internal x.AddCausalTokenElement(contextInformation:ContextInformation, elementType:GVT) =
        let ci = contextInformation
        let elementType = elementType ||| GVT.CausalToken
        let ctes = helper._causalTokenElements
        if ctes.ContainsKey(ci) then
            ctes[ci] <- (ctes[ci] ||| elementType)
        else
            ctes.Add(ci, elementType)
        //logDebug $"Added Element: {ci}={ctes[ci]}"


    member internal _.AppendPathElement(name:string) = helper.AppendPathElement(name)
    member internal _.AppendPathElement(names:Fqdn)  = helper.AppendPathElement(names)
    member internal _.CurrentPathElements = helper.CurrentPathElements
    member internal x.UpdateModelSpits() = helper.UpdateModelSpits()



    override x.EnterSystem(ctx:SystemContext) =
        //let name = ctx.systemName().GetText().DeQuoteOnDemand()
        //let theSystem = helper.TheSystem

        //let ns = collectSystemNames ctx

        //helper.TheSystem <-
        //    if theSystem.NameComponents = ns then
        //        helper.theSystem
        //    else
        //        theSystem.Systems.TryFind(fun sys -> sys.NameComponents = ns)

        ////match x._currentSystem with
        ////| None ->
        ////    let ns = collectNameComponents ctx
        ////    if theSystem.NameComponents = ns then
        ////        helper._currentSystem <- helper._theSystem
        ////    else
        ////        helper._currentSystem <- theSystem.Systems.TryFind(fun sys -> sys.NameComponents = ns)
        ////| Some curSys ->
        ////    helper._currentSystem <- Some <| curSys.Systems.Find(fun s -> s.Name = name)

        //assert (helper._currentSystem.IsSome)

        x.UpdateModelSpits()

    //override x.ExitSystem(ctx:SystemContext) = helper._currentSystem <- None

    override x.EnterFlow(ctx:FlowContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        let flow = helper.TheSystem.Value.Flows.TryFind(fun f -> f.Name = flowName)
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

