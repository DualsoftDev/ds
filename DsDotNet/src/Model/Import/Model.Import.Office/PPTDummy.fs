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
        let vertex = HashSet<Vertex>()
        let dummyEdges = HashSet<ModelingEdgeInfo<string>>()
        let dummyNode  = $"{page}_{shapeName}" 
        
        member x.Page = page
        member x.DummyNodeKey = dummyNode
        member x.Edges = dummyEdges
        member x.Members = vertex

        member x.AddOutEdge(edgeType:ModelingEdgeType, target:string) =
            x.Edges.Add(ModelingEdgeInfo(dummyNode, edgeType.ToText(), target)) |> ignore 
        member x.AddInEdge(edgeType:ModelingEdgeType, source:string) =
            x.Edges.Add(ModelingEdgeInfo(source, edgeType.ToText(), dummyNode)) |> ignore 

        member val internal DicVertex  = Dictionary<string, Vertex>() with get,set
        member val internal Items  = pptNodes
        member x.GetVertex(key:string) = 
                x.DicVertex
                 .Where(fun f->f.Key = key)
                 .Select(fun s->s.Value).FirstOrDefault()

        member x.GetParent() = vertex.First().Parent
        member x.Update(dic:Dictionary<string, Vertex>) =
                x.DicVertex <- dic
                pptNodes.Iter(fun f->  vertex.Add(x.DicVertex.[f.Key])|>ignore)
      


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
               



