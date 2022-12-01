// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTUtil
open Engine.Common.FS
open Model.Import.Office
open System.Collections.Generic
open Engine.Core
open System.Runtime.CompilerServices



[<AutoOpen>]
module PPTDummyModule =
    
    ///ppt더미 구성 Node
    type pptDummy(shapeName:string, page:int)  =
        
        let pptNodes = HashSet<pptNode>()
        let vertices = HashSet<Vertex>()
        let dummyEdges = HashSet<ModelingEdgeInfo<string>>()
        let mutable dicVertex  = Dictionary<string, Vertex>()
        let dummyNode  = $"{page}_{shapeName}" 
        let getVertexEdge(edge:ModelingEdgeInfo<string>)  = 
                let src = if dicVertex.ContainsKey(edge.Sources[0]) then dicVertex.[edge.Sources[0]].Name else edge.Sources[0]
                let tgt = if dicVertex.ContainsKey(edge.Targets[0]) then dicVertex.[edge.Targets[0]].Name else edge.Targets[0]
                ModelingEdgeInfo<string>(src , edge.EdgeSymbol, tgt)
                
        member x.Page = page
        member x.DummyNodeKey = dummyNode
        member x.Edges = dummyEdges //|> Seq.map(fun f-> getVertexEdge f)
        member x.Members = vertices

        member x.GetVertex(name:string) = 
            dicVertex.Where(fun f->f.Key = name).Select(fun s->s.Value).FirstOrDefault()
        member x.AddOutEdge(edgeType:ModelingEdgeType, target:string) =
            dummyEdges.Add(ModelingEdgeInfo<string>(dummyNode, edgeType.ToText(), target)) |> ignore 
        member x.AddInEdge(edgeType:ModelingEdgeType, source:string) =
            dummyEdges.Add(ModelingEdgeInfo<string>(source, edgeType.ToText(), dummyNode)) |> ignore 
       
       
        member x.GetParent() = vertices.First().Parent
        member val internal Items  = pptNodes
        member x.Update(dic:Dictionary<string, Vertex>) =
                dicVertex <- dic
                pptNodes.Iter(fun f->  vertices.Add(dicVertex.[f.Key])|>ignore)
      


[<Extension>]
type PPTDummyExt =
    [<Extension>] static  member TryFindDummy(dummys:HashSet<pptDummy>, pptNode:pptNode) = 
                    let findDummy = dummys.Where(fun w->w.Items.Contains(pptNode))
                    if findDummy.Count() > 1 then failwithf $"pptNode : {pptNode.Name} 복수의 dummy에 포함"
                    findDummy.FirstOrDefault()

    [<Extension>] static member IsMember(dummys:HashSet<pptDummy>, pptNode:pptNode) = 
                    let findDummy = dummys.TryFindDummy(pptNode)
                    findDummy.IsNonNull()

    [<Extension>] static member GetMembers(dummys:HashSet<pptDummy>, pptNode:pptNode) = 
                    let findDummy = dummys.TryFindDummy(pptNode)
                    if findDummy.IsNonNull()
                    then findDummy.Items   
                    else HashSet<pptNode>()

    [<Extension>] static member AddDummys(dummys:HashSet<pptDummy>, pptNodes:pptNode seq) = 
                    //dummy에 속한 node를 먼저 찾음
                    let findNode =  
                        pptNodes 
                        |> Seq.filter(fun pptNode -> dummys.TryFindDummy(pptNode).IsNonNull())

                    if findNode.Any()
                    then //findNode 자신을 제외한 나머지 등록
                        let findDummy = dummys.TryFindDummy(findNode.First())
                        pptNodes.Except(findNode)
                                .ForEach(fun f-> findDummy.Items.Add(f)|>ignore)
                    else 
                        let dummyNode = pptNodes.First();
                        let newDummy = pptDummy(dummyNode.Shape.ShapeName(), dummyNode.PageNum);
                        pptNodes.ForEach(fun f-> newDummy.Items.Add(f)|>ignore)
                        dummys.Add(newDummy) |> ignore
    
