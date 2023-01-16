// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office


open Engine.Common.FS
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module rec ViewModule = 

   
    type ViewNode(name:string, viewType:ViewType, coreVertex:Vertex option, btnType:BtnType option, lampType:LampType option)  = 

        new (viewType) = ViewNode("", viewType, None, None, None)
        new (name, viewType) = ViewNode(name, viewType, None, None, None)
        new (coreVertex:Vertex) = 
              let name = 
                  match coreVertex  with
                    | :? Alias as a -> match a.TargetWrapper with
                                       | DuAliasTargetReal r -> r.Name
                                       | DuAliasTargetCall c -> c.Name
                                       | DuAliasTargetRealEx o -> o.Name
                    | _ -> coreVertex.Name
                
              ViewNode(name, VREAL, Some(coreVertex),  None, None)
            
        new (name, btnType:BtnType) = ViewNode(name, VBUTTON, None, Some(btnType), None)
        new (name, lampType:LampType) = ViewNode(name, VLAMP, None, None, Some(lampType))

        member val Edges = HashSet<ModelingEdgeInfo<ViewNode>>()
        member val Singles = HashSet<ViewNode>()
        member val Status4 = Status4.Homing with get, set
        member val ViewType = viewType with get, set
        member val Flow:Flow option = None with get, set
        member val Page = 0 with get, set

        member x.DummyAdded = x.Edges |> Seq.collect(fun e-> e.Sources @ e.Targets)
                                      |> Seq.filter(fun v -> v.ViewType = VDUMMY)
                                      |> Seq.isEmpty |> not

        member x.CoreVertex = coreVertex
        member x.BtnType =  btnType
        member x.LampType =  lampType
        member x.IsChildExist =  x.Edges.Count>0 || x.Singles.Count>0
        member x.Name =  name
        member x.UIKey = if coreVertex.IsSome
                         then $"{name};{coreVertex.Value.QualifiedName.GetHashCode()}"
                         else $"{name};{x.GetHashCode()}"

        member x.UsedViewNodes = 
                            let thisChildren  = x.Edges |> Seq.collect(fun e-> e.Sources @ e.Targets)
                                                        |> Seq.append x.Singles
                            [
                                yield! thisChildren 
                                yield! thisChildren |> Seq.collect(fun x->x.UsedViewNodes)
                            ]
    

    let getViewEdge(edge:ModelingEdgeInfo<string> ,dummy:pptDummy , dummys:pptDummy seq,  dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =  
        let getVertex(dummyKey:string) =  
            let createDummy() =  
                if dicDummy.ContainsKey(dummyKey) then dicDummy.[dummyKey]
                else
                     let viewNode = ViewNode(ViewType.VDUMMY)
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
    static member GetDummyFlow(flow:Flow, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =  
            dummys 
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = flow)
            |> Seq.collect(fun dummy -> dummy.Edges 
                                        |> Seq.map(fun edge -> getViewEdge(edge, dummy, dummys, dicV, dicDummy)))

    [<Extension>] 
    static member GetDummyReal(real:Real, dummys:pptDummy seq, dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =  
            dummys 
            |> Seq.filter(fun dummy  -> dummy.GetParent().GetCore() = real)
            |> Seq.collect(fun dummy -> dummy.Edges 
                                        |> Seq.map(fun edge -> getViewEdge(edge, dummy, dummys, dicV, dicDummy)))
           
    [<Extension>] 
    static member GetDummyMembers(dummys:pptDummy seq) =   dummys  |> Seq.collect(fun f-> f.Members)
     