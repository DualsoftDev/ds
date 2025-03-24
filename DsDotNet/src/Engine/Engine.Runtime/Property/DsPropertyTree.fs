namespace Engine.Runtime

open System
open System.IO
open System.Runtime.CompilerServices
open Newtonsoft.Json
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module DsPropertyTreeModule =

    type DsTreeNode = {
        Node: PropertyBase
        Children: DsTreeNode list
    }

    let CreateTreeNode(node: PropertyBase, children: DsTreeNode list) : DsTreeNode =
        { Node = node; Children = children }

    let [<Literal>] InputParameter = "Input Parameter"
    let [<Literal>] OutputParameter = "Output Parameter"

[<Extension>]
type DsPropertyTreeExt =

    [<Extension>]
    static member GetPropertyTreeFromSystem(sys:DsSystem) =
        // 트리 구조 생성 함수
        let rec buildFlowTree (flow: Flow) =
            let vertices = flow.Graph.Vertices |> Seq.map buildVertexTree |> Seq.toList
            CreateTreeNode(PropertyFlow(flow), vertices)

        and createJobTask(call:Call) =
            let taskDevNodes = call.TargetJob.TaskDefs 
                                    |> Seq.map (fun taskDef -> 
                                            CreateTreeNode(PropertyTaskDev(taskDef), []) )
                                    |> Seq.toList
            let jobNode = CreateTreeNode(PropertyJob(call.TargetJob), taskDevNodes)
            CreateTreeNode(PropertyCall(call), [jobNode])

        and buildVertexTree vertex =
            match vertex with
            | :? Call as call when call.IsJob  ->  createJobTask call
            | :? Call as call when call.IsOperator ->   CreateTreeNode(PropertyOperatorFunction(call.TargetFunc.Value :?> OperatorFunction), [])
            | :? Real as real -> 
                let coins = real.Graph.Vertices |> Seq.map buildCoinTree |> Seq.toList
                CreateTreeNode(PropertyReal(real), coins)
            | :? Alias as alias -> CreateTreeNode(PropertyAlias(alias), [])
            | _ -> failwith $"Unknown vertex type: {vertex.DequotedQualifiedName}"

        and buildCoinTree coin =
            match coin with
            | :? Call as call when call.IsCommand -> 
                CreateTreeNode(PropertyCommandFunction(call.TargetFunc.Value :?> CommandFunction), [])
            | :? Call as call when call.IsOperator -> 
                CreateTreeNode(PropertyOperatorFunction(call.TargetFunc.Value :?> OperatorFunction), [])
            | :? Call as call when call.IsJob ->  createJobTask call
                
            | :? Alias as alias -> CreateTreeNode(PropertyAlias(alias), [])
            | _ -> failwith $"Unknown coin type: {coin.DequotedQualifiedName}"


        // 그룹 생성
        let flowGroup = CreateTreeNode(PropertyBase("Flows"), sys.Flows |> Seq.map buildFlowTree |> Seq.toList)
        flowGroup

    [<Extension>]
    static member GetPropertyTree(model: RuntimeModel) : DsTreeNode =

        let buildModelConfigTree (modelConfig: ModelConfig) : DsTreeNode =
            CreateTreeNode(PropertyModelConfig(modelConfig), [])
        let buildHwSystemTree (hwSystemDef: HwSystemDef) : DsTreeNode =
            CreateTreeNode(PropertyHwSystemDef(hwSystemDef), [])

        let flowGroup = DsPropertyTreeExt.GetPropertyTreeFromSystem model.System
        let hwGroup = CreateTreeNode(PropertyBase("Utils"), model.System.HwSystemDefs |> Seq.map buildHwSystemTree |> Seq.toList)
        let dummyGroup = CreateTreeNode(PropertyBase("Settings"), [model.ModelConfig |> buildModelConfigTree])
        // 최상위 시스템 노드 생성
        CreateTreeNode(PropertySystem(model.System), [flowGroup; hwGroup; dummyGroup])
