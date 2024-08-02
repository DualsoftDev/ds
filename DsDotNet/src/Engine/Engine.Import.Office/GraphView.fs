// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open Dual.Common.Core.FS
open Engine.Core
open Engine.CodeGenCPU
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Linq
open System.ComponentModel
open System

[<AutoOpen>]
module rec ViewModule =


    type ViewNode
        (
            name: string,
            viewType: ViewType,
            coreVertex: Vertex option,
            btnType: BtnType option,
            lampType: LampType option,
            cType: ConditionType option
        ) =
        let dicUsedVertex = new Dictionary<Vertex, ViewNode>()
        let edges = HashSet<ModelingEdgeInfo<ViewNode>>()
        let singles = HashSet<ViewNode>()
        let mutable goingCont = 0


        let usedViewNodes () =
            let thisChildren =
                edges |> Seq.collect (fun e -> e.Sources @ e.Targets) |> Seq.append singles

            [ yield! thisChildren
              yield! thisChildren |> Seq.collect (fun x -> x.UsedViewNodes)

              ]
            |> Seq.distinct


        let callSafeAutoPreName (call: Call) =
            let getNameNValue(sa:SafetyAutoPreCondition) =  
                let condiJob = sa.GetJob()
                let values = String.Join(","
                    , condiJob
                        .TaskDefs
                        .Select(fun td ->
                            if td.InTag.IsNonNull()
                            then 
                                match getDataType td.InTag.DataType  with
                                | DuBOOL -> 
                                    if Convert.ToBoolean(td.InTag.BoxedValue)
                                    then "T" else "F"
                                | _-> td.InTag.BoxedValue.ToString()
                            else 
                                let value  = (td.TagManager:?>TaskDevManager).PlanEnd(condiJob).Value
                                if Convert.ToBoolean(value)
                                then "T" else "F"
                            ))

                if values = ""
                then
                    failWithLog $"Error tag address {sa.GetJob().QualifiedName}"

                let jobFlow = condiJob.NameComponents.Head()
                if call.Parent.GetFlow().Name = jobFlow
                then 
                    $"{condiJob.NameComponents.Skip(1).Combine()} {values}"
                else 
                    $"{condiJob.DequotedQualifiedName} {values}"
                    
            let safeties = String.Join(", ", call.SafetyConditions.Select(fun f->getNameNValue f))
            let safeName = if safeties.Length > 0 then $"[[{safeties}]]\r\n" else ""

            let autoPres = String.Join(", ", call.AutoPreConditions.Select(fun f->getNameNValue f))
            let autoPresName = if autoPres.Length > 0 then $"[{autoPres}]\r\n" else ""

            $"{safeName}{autoPresName}"


        new(name, viewType) = ViewNode(name, viewType, None, None, None, None)

        new(coreVertex: Vertex) =
            let name, vType =
                match coreVertex with
                | :? Alias as a ->
                    match a.TargetWrapper with
                    | DuAliasTargetReal r -> r.Name, VREAL
                    | DuAliasTargetCall c -> c.Name, VCALL
                | :? Call as c -> c.Name, VCALL
                | :? Real as c -> c.Name, VREAL
                | _ -> coreVertex.Name, VFLOW

            ViewNode(name, vType, Some(coreVertex), None, None, None)

        new(name, btnType: BtnType) = ViewNode(name, VBUTTON, None, Some(btnType), None, None)
        new(name, lampType: LampType) = ViewNode(name, VLAMP, None, None, Some(lampType), None)
        new(name, cType: ConditionType) = ViewNode(name, VCONDITION, None, None, None, Some(cType))


        [<Browsable(false)>]
        member val Status4 = Status4.Homing with get, set

        member val ViewType = viewType with get, set

        [<Browsable(false)>]
        member val Flow: Flow option = None with get, set


        member x.IsVertex =
            viewType = ViewType.VREAL
            || viewType = ViewType.VCALL

        member x.GetEdges() = edges.ToArray()

        member x.AddEdge(edge: ModelingEdgeInfo<ViewNode>) =
            edges.Add edge |> ignore
            x.UsedViewVertexNodes(true) |> ignore

        member x.GetSingles() = singles.ToArray()

        member x.AddSingles(single: ViewNode) =
            singles.Add single |> ignore
            x.UsedViewVertexNodes(true) |> ignore


        [<Browsable(false)>]
        member x.DummyEdgeAdded =
            edges
            |> Seq.collect (fun e -> e.Sources @ e.Targets)
            |> Seq.filter (fun v -> v.ViewType = VDUMMY)
            |> Seq.any

        [<Browsable(false)>]
        member x.DummySingleAdded =
            singles |> Seq.filter (fun v -> v.ViewType = VDUMMY) |> Seq.any

        [<Browsable(false)>]
        member x.CoreVertex = coreVertex

        [<Browsable(false)>]
        member x.PureVertex =
            if coreVertex.IsSome then
                coreVertex.Value.GetPure() |> Some
            else
                None

        [<Browsable(false)>]
        member x.BtnType = btnType

        [<Browsable(false)>]
        member x.LampType = lampType

        [<Browsable(false)>]
        member x.ConditionType = cType

        [<Browsable(false)>]
        member x.IsChildExist = edges.Count > 0 || singles.Count > 0

        member x.Name = name
        member x.UpdateGoingCnt() = goingCont <- goingCont+1
        member x.GoingCnt = 
            if coreVertex.IsNull()
            then 0u
            else 
                match coreVertex.Value.GetPure() with
                | :? Real as r -> r.RealSEQ
                |_-> 0u
     
        member x.DisplayName =   
            if coreVertex.IsSome then   
                let safeAutoPre = match coreVertex.Value.GetPure() with
                                   | :? Call as c -> callSafeAutoPreName c
                                   | :? Real -> ""
                                   |_-> failwithlog $"Error {coreVertex.Value.Name}"

                match coreVertex.Value with
                | :? Alias as a ->
                    if a.IsSameFlow
                    then
                        $"{safeAutoPre}{name}"
                    else 
                        $"{safeAutoPre}{a.GetPure().ParentNPureNames.Combine()}"  
                | _ -> $"{safeAutoPre}{x.PureVertex.Value.Name}"
            else 
                $"{name}"

        [<Browsable(false)>]
        member x.UIKey =
                if coreVertex.IsSome then   
                    coreVertex.Value.QualifiedName.GetHashCode().ToString()
                else 
                    x.GetHashCode().ToString()


        [<Browsable(false)>]
        member x.UsedViewNodes = usedViewNodes ()

        [<Browsable(false)>]
        member x.UsedViewVertexNodes() = x.UsedViewVertexNodes(false)

        [<Browsable(false)>]
        member private x.UsedViewVertexNodes(newDic: bool) =
            if
                newDic || dicUsedVertex.Count = 0 //성능 때문에 한번만 만듬 //Edges  Singles 업데이트될 경우 같이 업데이트 필요
            then
                dicUsedVertex.Clear()
                let used = x.UsedViewNodes

                used
                |> Seq.where (fun f -> f.CoreVertex.IsSome)
                |> Seq.iter (fun f -> dicUsedVertex.Add(f.CoreVertex.Value, f))

            dicUsedVertex


    let getViewEdge
        (
            edge: ModelingEdgeInfo<string>,
            dummy: pptDummy,
            dummys: pptDummy seq,
            dicV: IDictionary<Vertex, ViewNode>,
            dicDummy: IDictionary<string, ViewNode>
        ) =
        let getVertex (dummyKey: string) =
            let createDummy () =
                if dicDummy.ContainsKey(dummyKey) then
                    dicDummy.[dummyKey]
                else
                    let viewNode = ViewNode("", ViewType.VDUMMY)
                    let dummy = dummys.First(fun f -> f.DummyNodeKey = dummyKey)

                    dummy.Members |> Seq.iter (fun f -> viewNode.AddSingles(dicV.[f]) |> ignore)
                    dicDummy.Add(dummyKey, viewNode) |> ignore
                    viewNode

            let findV = dummy.GetVertex(dummyKey)

            let vertex =
                if findV.IsSome then
                    dicV.[findV.Value.Value]
                else
                    createDummy ()

            vertex

        let src = getVertex (edge.Sources[0])
        let tgt = getVertex (edge.Targets[0])
        ModelingEdgeInfo<ViewNode>(src, edge.EdgeSymbol, tgt)

[<Extension>]
type ViewModuleExt =
    [<Extension>]
    static member GetDummyEdgeFlow
        (
            flow: Flow,
            dummys: pptDummy seq,
            dicV: IDictionary<Vertex, ViewNode>,
            dicDummy: IDictionary<string, ViewNode>
        ) =
        dummys
        |> Seq.filter (fun dummy -> dummy.GetParent().GetCore() = flow)
        |> Seq.collect (fun dummy ->
            dummy.DummyEdges
            |> Seq.map (fun edge -> getViewEdge (edge, dummy, dummys, dicV, dicDummy)))

    [<Extension>]
    static member GetDummySingleFlow
        (
            flow: Flow,
            dummys: pptDummy seq,
            dicV: IDictionary<Vertex, ViewNode>,
            dicDummy: IDictionary<string, ViewNode>
        ) =
        let connectedDummys =
            dummys
            |> Seq.collect (fun d -> d.DummyEdges)
            |> Seq.collect (fun e -> [ e.Sources[0]; e.Targets[0] ])
            |> Seq.distinct

        dummys
        |> Seq.filter (fun dummy -> dummy.GetParent().GetCore() = flow)
        |> Seq.filter (fun dummy -> not <| connectedDummys.Contains(dummy.DummyNodeKey))
        |> Seq.map (fun dummy ->
            let viewNode = ViewNode("", ViewType.VDUMMY)
            dummy.Members |> Seq.iter (fun f -> viewNode.AddSingles(dicV.[f]) |> ignore)
            viewNode)



    [<Extension>]
    static member GetDummyEdgeReal
        (
            real: Real,
            dummys: pptDummy seq,
            dicV: IDictionary<Vertex, ViewNode>,
            dicDummy: IDictionary<string, ViewNode>
        ) =
        dummys
        |> Seq.filter (fun dummy -> dummy.GetParent().GetCore() = real)
        |> Seq.collect (fun dummy ->
            dummy.DummyEdges
            |> Seq.map (fun edge -> getViewEdge (edge, dummy, dummys, dicV, dicDummy)))

    [<Extension>]
    static member GetDummySingleReal
        (
            real: Real,
            dummys: pptDummy seq,
            dicV: IDictionary<Vertex, ViewNode>,
            dicDummy: IDictionary<string, ViewNode>
        ) =
        let connectedDummys =
            dummys
            |> Seq.collect (fun d -> d.DummyEdges)
            |> Seq.collect (fun e -> [ e.Sources[0]; e.Targets[0] ])
            |> Seq.distinct


        dummys
        |> Seq.filter (fun dummy -> dummy.GetParent().GetCore() = real)
        |> Seq.filter (fun dummy -> not <| connectedDummys.Contains(dummy.DummyNodeKey))
        |> Seq.map (fun dummy ->
            let viewNode = ViewNode("", ViewType.VDUMMY)
            dummy.Members |> Seq.iter (fun f -> viewNode.AddSingles(dicV.[f]) |> ignore)
            viewNode)



    [<Extension>]
    static member GetDummyMembers(dummys: pptDummy seq) =
        dummys |> Seq.collect (fun f -> f.Members)
