namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open Engine.Core
open System

[<AutoOpen>]
module ImportUtilVertex =


    let getOperatorFunc (sys: DsSystem) (node: PptNode) =
        if sys.Functions.OfType<CommandFunction>().any(fun f->f.Name = node.OperatorName)
        then
            failWithLog $"Function name ({node.OperatorName}) is already used as a command name"
        else
            sys.Functions.OfType<OperatorFunction>()
            |> Seq.tryFind (fun f -> f.Name = node.OperatorName)
            |> Option.defaultWith (fun () ->
                let newFunc = OperatorFunction(node.OperatorName)
                sys.Functions.Add newFunc |> ignore
                newFunc
            )

    let getCommandFunc (sys: DsSystem) (node: PptNode) =
        if sys.Functions.OfType<OperatorFunction>().any(fun f->f.Name = node.CommandName)
        then
            failWithLog $"Function name ({node.CommandName}) is already used as an operator name"
        else
        sys.Functions.OfType<CommandFunction>()
        |> Seq.tryFind (fun f -> f.Name = node.CommandName)
        |> Option.defaultWith (fun () ->
            let newFunc = CommandFunction(node.CommandName)
            sys.Functions.Add newFunc |> ignore
            newFunc
        )

    let getCallFromLoadedSys (sys: DsSystem) (device: LoadedSystem) (node: PptNode) (parentWrapper:ParentWrapper) =
        let loadSysName = device.Name
        let jobName = node.Job.Combine()
        let apiName = node.ApiName

        match sys.Jobs |> Seq.tryFind (fun job -> job.DequotedQualifiedName = jobName) with
        | Some job ->
            parentWrapper.CreateCall(job, node.ValueParamIO)
        | None ->
            match device.ReferenceSystem.ApiItems |> Seq.tryFind (fun a -> a.PureName = node.ApiName) with
            |Some api ->
                let devTask =
                    match sys.TaskDevs.TryFind(fun d->d.ApiItem = api) with
                    | Some (taskDev) ->
                        taskDev
                    | _ ->
                        TaskDev(api, loadSysName, sys)

                let job = Job(node.Job, sys, [devTask])
                updateAddressSkip( node.JobParam, job)
                sys.Jobs.Add job |> ignore
                parentWrapper.CreateCall(job, node.ValueParamIO)

            | None ->
                if device.AutoGenFromParentSystem
                then
                    let autoTaskDev = getAutoGenTaskDev device loadSysName  apiName
                    let job = Job(node.Job, sys, [autoTaskDev])
                    updateAddressSkip( node.JobParam, job)
                    sys.Jobs.Add job |> ignore
                    parentWrapper.CreateCall(job, node.ValueParamIO)
                else
                    let ableApis = String.Join(", ", device.ReferenceSystem.ApiItems.Select(fun a->a.Name))
                    failwithlog $"Loading system ({loadSysName}:{device.AbsoluteFilePath}) \r\napi ({apiName}) not found \r\nApi List : {ableApis}"



    let private createCall (mySys: DsSystem, node: PptNode, parentWrapper: ParentWrapper, platformTarget:PlatformTarget) =
        match  mySys.LoadedSystems.TryFind(fun d -> d.Name = $"{node.DevName}") with
            |  Some dev ->
                getCallFromLoadedSys mySys dev node parentWrapper
            | _  ->
                let callParams = {
                    MySys = mySys
                    Node = node
                    JobName = node.Job.CombineQuoteOnDemand()
                    DevName = node.DevName
                    ApiName = node.ApiName
                    ValueParamIO = node.ValueParamIO
                    Parent = parentWrapper
                    PlatformTarget = platformTarget
                    }
                addNewCall callParams

    let createCallVertex (mySys: DsSystem, node: PptNode, parentWrapper: ParentWrapper, platformTarget:PlatformTarget, dicSeg: Dictionary<string, Vertex>) =
        let call =
            if node.IsFunction then
                if node.IsRootNode.Value then
                    parentWrapper.CreateCall(getOperatorFunc mySys node)
                else
                    parentWrapper.CreateCall(getCommandFunc mySys node)
            else
                match mySys.Flows.TryFind(fun f-> f.Name = node.Job.Head()) with
                | Some _ ->
                    createCall (mySys, node, parentWrapper, platformTarget)
                | None ->   failWithLog $"Flow not found : {node.Job.Head()}"

        node.UpdateCallProperty(call)
        dicSeg.Add(node.Key, call)


    let  getParent
        (
            edge: PptEdge,
            parents: IDictionary<PptNode, seq<PptNode>>,
            dicSeg: Dictionary<string, Vertex>
        ) =
        ImportDocCheck.SameParent(parents, edge)

        let newParents =
            parents
            |> Seq.filter (fun group -> group.Value.Contains(edge.StartNode) && group.Value.Contains(edge.EndNode))
            |> Seq.map (fun group -> dicSeg.[group.Key.Key])

        if (newParents.Any() && newParents.length () > 1) then
            failwithlog "중복부모"

        if (newParents.Any()) then
            Some(newParents |> Seq.head)
        else
            None


    let getOtherFlowReal (flows: Flow seq, nodeEx: PptNode) =
        let flowName, nodeName = nodeEx.Name.Split('.')[0], nodeEx.Name.Split('.')[1]

        match flows.TryFind(fun f -> f.Name = flowName) with
        | Some flow ->
            match flow.Graph.Vertices.TryFind(fun f -> f.Name = nodeName) with
            | Some real -> real
            | None -> nodeEx.Shape.ErrorName($"{ErrID._27} Error Name : [{nodeName}]", nodeEx.PageNum)
        | None -> nodeEx.Shape.ErrorName($"{ErrID._26} Error Name : [{flowName}]", nodeEx.PageNum)
