// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office


open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Linq
open System.ComponentModel

[<AutoOpen>]
module rec ViewModule =


    type ViewNode(name:string, viewType:ViewType, coreVertex:Vertex option, btnType:BtnType option, lampType:LampType option, cType:ConditionType option)  =

        new (name, viewType) = ViewNode(name, viewType, None, None, None, None)
        new (coreVertex:Vertex) =
              let name, vType =
                  match coreVertex  with
                    | :? Alias as a -> match a.TargetWrapper with
                                       | DuAliasTargetReal r -> r.Name , VREAL
                                       | DuAliasTargetCall c -> c.Name , VCALL
                                       | DuAliasTargetRealExFlow rf -> rf.Name , VREALEx
                                       | DuAliasTargetRealExSystem rs -> rs.Name , VREALEx
                    | :? Call as c -> c.Name, VCALL
                    | :? Real as c -> c.Name, VREAL
                    | :? RealExF as c -> c.Name, VREALEx
                    | _ -> coreVertex.Name, VFLOW

              ViewNode(name, vType, Some(coreVertex),  None, None, None)

        new (name, btnType:BtnType) = ViewNode(name, VBUTTON, None, Some(btnType), None, None)
        new (name, lampType:LampType) = ViewNode(name, VLAMP, None, None, Some(lampType), None)
        new (name, cType:ConditionType) = ViewNode(name, VCONDITION, None, None, None, Some(cType))

        [<Browsable(false)>]
        member val Edges = HashSet<ModelingEdgeInfo<ViewNode>>()
        [<Browsable(false)>]
        member val Singles = HashSet<ViewNode>()
        [<Browsable(false)>]
        member val Status4 = Status4.Homing with get, set
        member val ViewType = viewType with get, set
        [<Browsable(false)>]
        member val Flow:Flow option = None with get, set
        [<ReadOnly(true)>]
        member val Page = 0 with get, set

        [<Browsable(false)>]
        member x.DummyEdgeAdded = x.Edges |> Seq.collect(fun e-> e.Sources @ e.Targets)
                                          |> Seq.filter(fun v -> v.ViewType = VDUMMY)
                                          |> Seq.any
        [<Browsable(false)>]
        member x.DummySingleAdded = x.Singles 
                                          |> Seq.filter(fun v -> v.ViewType = VDUMMY)
                                          |> Seq.any
        [<Browsable(false)>]
        member x.CoreVertex = coreVertex
        [<Browsable(false)>]
        member x.BtnType =  btnType
        [<Browsable(false)>]
        member x.LampType =  lampType
        [<Browsable(false)>]
        member x.ConditionType =  cType
        [<Browsable(false)>]
        member x.IsChildExist =  x.Edges.Count>0 || x.Singles.Count>0
        member x.Name =  name
        [<Browsable(false)>]
        member x.UIKey = if coreVertex.IsSome
                         then $"{name};{coreVertex.Value.QualifiedName.GetHashCode()}"
                         else $"{name};{x.GetHashCode()}"

        [<Browsable(false)>]
        member x.UsedViewNodes =
                            let thisChildren  = x.Edges |> Seq.collect(fun e-> e.Sources @ e.Targets)
                                                        |> Seq.append x.Singles
                            [
                                yield! thisChildren
                                yield! thisChildren |> Seq.collect(fun x->x.UsedViewNodes)
                            ] |> Seq.distinct


    let getViewEdge(edge:ModelingEdgeInfo<string> ,dummy:pptDummy , dummys:pptDummy seq,  dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
        let getVertex(dummyKey:string) =
            let createDummy() =
                if dicDummy.ContainsKey(dummyKey) then dicDummy.[dummyKey]
                else
                     let viewNode = ViewNode("", ViewType.VDUMMY)
                     let dummy  = dummys.First(fun f-> f.DummyNodeKey = dummyKey)

                     dummy.Members |> Seq.iter(fun f-> viewNode.Singles.Add(dicV.[f])|>ignore)
                     dicDummy.Add(dummyKey, viewNode) |>ignore
                     viewNode
            let findV = dummy.GetVertex(dummyKey);
            let vertex = if findV.IsNonNull() then dicV.[findV] else createDummy()
            vertex

        let src = getVertex (edge.Sources[0]);
        let tgt = getVertex (edge.Targets[0]);
        ModelingEdgeInfo<ViewNode>(src, edge.EdgeSymbol, tgt)

[<Extension>]
type ViewModuleExt =
    [<Extension>]
    static member GetDummyEdgeFlow(flow:Flow, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = flow)
            |> Seq.collect(fun dummy -> dummy.Edges
                                        |> Seq.map(fun edge -> getViewEdge(edge, dummy, dummys, dicV, dicDummy)))

    [<Extension>]
    static member GetDummySingleFlow(flow:Flow, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = flow)
            |> Seq.filter(fun dummy  -> not <| dummy.Edges.any())
            |> Seq.map(fun dummy ->
                    let viewNode = ViewNode("", ViewType.VDUMMY)
                    dummy.Members |> Seq.iter(fun f-> viewNode.Singles.Add(dicV.[f])|>ignore)
                    viewNode
                    )


                                        
    [<Extension>]
    static member GetDummyEdgeReal(real:Real, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = real)
            |> Seq.collect(fun dummy -> dummy.Edges
                                        |> Seq.map(fun edge -> getViewEdge(edge, dummy, dummys, dicV, dicDummy)))
    [<Extension>]
    static member GetDummySingleReal(real:Real, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = real)
            |> Seq.filter(fun dummy  -> not <| dummy.Edges.any())
            |> Seq.map(fun dummy ->
                    let viewNode = ViewNode("", ViewType.VDUMMY)
                    dummy.Members |> Seq.iter(fun f-> viewNode.Singles.Add(dicV.[f])|>ignore)
                    viewNode
                    )

                                        

    [<Extension>]
    static member GetDummyMembers(dummys:pptDummy seq) =   dummys  |> Seq.collect(fun f-> f.Members)
