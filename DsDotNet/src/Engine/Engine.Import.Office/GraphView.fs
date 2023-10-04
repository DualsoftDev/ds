// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office


open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Linq
open System.ComponentModel

[<AutoOpen>]
module rec ViewModule =


    type ViewNode(name:string, viewType:ViewType, coreVertex:Vertex option, btnType:BtnType option, lampType:LampType option, cType:ConditionType option)  =
        let dicUsedVertex = new Dictionary<Vertex, ViewNode>()
        let edges = HashSet<ModelingEdgeInfo<ViewNode>>()
        let singles = HashSet<ViewNode>()


        let usedViewNodes() =        
            let thisChildren  = edges |> Seq.collect(fun e-> e.Sources @ e.Targets)
                                        |> Seq.append singles
            [
                yield! thisChildren
                yield! thisChildren |> Seq.collect(fun x->x.UsedViewNodes)

            ] |> Seq.distinct


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
        member val Status4 = Status4.Homing with get, set
        member val ViewType = viewType with get, set
        [<Browsable(false)>]
        member val Flow:Flow option = None with get, set
        [<ReadOnly(true)>]
        member val Page = 0 with get, set
        
        member x.GetEdges() = edges.ToArray()
        member x.AddEdge(edge:ModelingEdgeInfo<ViewNode>) =
                        edges.Add edge |> ignore
                        x.UsedViewVertexNodes(true) |> ignore

        member x.GetSingles() = singles.ToArray()
        member x.AddSingles(single:ViewNode) = 
                        singles.Add single |> ignore 
                        x.UsedViewVertexNodes(true) |> ignore
         

        [<Browsable(false)>]
        member x.DummyEdgeAdded = edges |> Seq.collect(fun e-> e.Sources @ e.Targets)
                                          |> Seq.filter(fun v -> v.ViewType = VDUMMY)
                                          |> Seq.any
        [<Browsable(false)>]
        member x.DummySingleAdded = singles 
                                          |> Seq.filter(fun v -> v.ViewType = VDUMMY)
                                          |> Seq.any
        [<Browsable(false)>]
        member x.CoreVertex = coreVertex
        [<Browsable(false)>]
        member x.PureVertex = if coreVertex.IsSome 
                              then coreVertex.Value.GetPure() |> Some
                              else None
        [<Browsable(false)>]
        member x.BtnType =  btnType
        [<Browsable(false)>]
        member x.LampType =  lampType
        [<Browsable(false)>]
        member x.ConditionType =  cType
        [<Browsable(false)>]
        member x.IsChildExist =  edges.Count>0 || singles.Count>0
        member x.Name =  name
        [<Browsable(false)>]
        member x.UIKey = if coreVertex.IsSome
                         then $"{name};{coreVertex.Value.QualifiedName.GetHashCode()}"
                         else $"{name};{x.GetHashCode()}"

        [<Browsable(false)>]
        member x.UsedViewNodes = usedViewNodes()

        [<Browsable(false)>]
        member x.UsedViewVertexNodes() = x.UsedViewVertexNodes(false)
        [<Browsable(false)>]
        member private x.UsedViewVertexNodes(newDic:bool) =
                        if newDic || dicUsedVertex.Count = 0  //성능 때문에 한번만 만듬 //Edges  Singles 업데이트될 경우 같이 업데이트 필요
                        then
                            dicUsedVertex.Clear()
                            let used = x.UsedViewNodes
                            used |> Seq.where(fun f->f.CoreVertex.IsSome)
                                 |> Seq.iter(fun f->  dicUsedVertex.Add( f.CoreVertex.Value, f))       
                         
                        dicUsedVertex


    let getViewEdge(edge:ModelingEdgeInfo<string> ,dummy:pptDummy , dummys:pptDummy seq,  dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
        let getVertex(dummyKey:string) =
            let createDummy() =
                if dicDummy.ContainsKey(dummyKey) then dicDummy.[dummyKey]
                else
                     let viewNode = ViewNode("", ViewType.VDUMMY)
                     let dummy  = dummys.First(fun f-> f.DummyNodeKey = dummyKey)

                     dummy.Members |> Seq.iter(fun f-> viewNode.AddSingles(dicV.[f])|>ignore)
                     dicDummy.Add(dummyKey, viewNode) |>ignore
                     viewNode
            let findV = dummy.GetVertex(dummyKey);
            let vertex = if findV.IsSome then dicV.[findV.Value.Value] else createDummy()
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
            |> Seq.collect(fun dummy -> dummy.DummyEdges
                                        |> Seq.map(fun edge -> getViewEdge(edge, dummy, dummys, dicV, dicDummy)))

    [<Extension>]
    static member GetDummySingleFlow(flow:Flow, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            let connectedDummys =
                dummys |> Seq.collect(fun d->d.DummyEdges) 
                       |> Seq.collect(fun e->[e.Sources[0];e.Targets[0]])
                       |> Seq.distinct

            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = flow)
            |> Seq.filter(fun dummy  -> not <| connectedDummys.Contains(dummy.DummyNodeKey))
            |> Seq.map(fun dummy ->
                    let viewNode = ViewNode("", ViewType.VDUMMY)
                    dummy.Members |> Seq.iter(fun f-> viewNode.AddSingles(dicV.[f])|>ignore)
                    viewNode
                    )


                                        
    [<Extension>]
    static member GetDummyEdgeReal(real:Real, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = real)
            |> Seq.collect(fun dummy -> dummy.DummyEdges
                                        |> Seq.map(fun edge -> getViewEdge(edge, dummy, dummys, dicV, dicDummy)))
    [<Extension>]
    static member GetDummySingleReal(real:Real, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =
            let connectedDummys =
                dummys |> Seq.collect(fun d->d.DummyEdges) 
                       |> Seq.collect(fun e->[e.Sources[0];e.Targets[0]])
                       |> Seq.distinct


            dummys
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = real)
            |> Seq.filter(fun dummy  -> not <| connectedDummys.Contains(dummy.DummyNodeKey))
            |> Seq.map(fun dummy ->
                    let viewNode = ViewNode("", ViewType.VDUMMY)
                    dummy.Members |> Seq.iter(fun f-> viewNode.AddSingles(dicV.[f])|>ignore)
                    viewNode
                    )

                                        

    [<Extension>]
    static member GetDummyMembers(dummys:pptDummy seq) =   dummys  |> Seq.collect(fun f-> f.Members)
