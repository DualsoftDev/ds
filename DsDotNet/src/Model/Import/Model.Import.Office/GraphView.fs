// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office


open Engine.Common.FS
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module rec ViewModule = 

   
    type ViewNode(name:string, nodeType:NodeType, coreVertex:Vertex option, btnType:BtnType option) = 
        let btnType:BtnType option = btnType
        let coreVertex:Vertex option = None

        new () = ViewNode("", MY, None, None)
        new (nodeType) = ViewNode("", nodeType, None, None)
        new (name, nodeType) = ViewNode(name, nodeType, None, None)
        new (name) = ViewNode(name, MY, None, None)
        new (coreVertex:Vertex) = 
              let name = 
                  match coreVertex  with
                    | :? Alias as a -> match a.Target with
                                       | AliasTargetReal r -> r.Name
                                       | AliasTargetCall c -> c.Name
                    | _ -> coreVertex.Name
                
              ViewNode(name, MY, Some(coreVertex),  None)
            
        new (name, btnType:BtnType) = ViewNode(name, BUTTON, None, Some(btnType))

        member val Edges = HashSet<ModelingEdgeInfo<ViewNode>>()
        member val Singles = HashSet<ViewNode>()
        member val NodeType = nodeType with get, set
        member val Flow:Flow option = None with get, set
        member val Page = 0 with get, set

        member x.CoreVertex =  coreVertex
        member x.BtnType =  btnType
        member x.IsChildExist =  x.Edges.Count>0 || x.Singles.Count>0
        member x.UIKey =  $"{name};{x.GetHashCode()}"
    

    let getViewEdge(edge:ModelingEdgeInfo<string> ,dummy:pptDummy , dummys:pptDummy seq,  dicV:IDictionary<Vertex, ViewNode>, dicDummy:IDictionary<string, ViewNode>) =  
        let getVertex(dummyKey:string) =  
            let createDummy() =  
                if dicDummy.ContainsKey(dummyKey) then dicDummy.[dummyKey]
                else
                     let viewNode = ViewNode(NodeType.DUMMY)
                     let dummy  = dummys.First(fun f-> f.DummyNodeKey = dummyKey)

                     dummy.Members |> Seq.iter(fun f-> viewNode.Singles.Add(dicV.[f])|>ignore)
                     dicDummy.Add(dummyKey, viewNode) |>ignore
                     viewNode
            let findV = dummy.GetVertex(dummyKey);
            let vertex = if findV.IsNonNull() then dicV.[findV] else createDummy()
            vertex
        
        let src = getVertex (edge.Source);
        let tgt = getVertex (edge.Target);
        ModelingEdgeInfo(src, edge.EdgeSymbol, tgt)

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
     