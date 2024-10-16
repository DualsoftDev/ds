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

    let getCallFromLoadedSys (sys: DsSystem) (device: LoadedSystem) (node: PptNode) parentWrapper =
        let loadSysName = device.Name
        let jobName = node.Job.Combine()
        let apiName = node.ApiPureName

        match sys.Jobs |> Seq.tryFind (fun job -> job.DequotedQualifiedName = jobName) with
        | Some job -> Call.Create(job, parentWrapper)
        | None ->
            match device.ReferenceSystem.ApiItems |> Seq.tryFind (fun a -> a.PureName = node.ApiPureName) with
            |Some api ->
                let devTask =
                    let TaskDevPara =  node.TaskDevParam

                    match sys.TaskDevs.TryFind(fun d->d.ApiItems.Contains(api)) with
                    | Some (taskDev) ->
                        taskDev.AddOrUpdateApiTaskDevParam(jobName, api, TaskDevPara)
                        taskDev
                    | _ ->
                        let apiPara  = {TaskDevParamIO =  TaskDevPara; ApiItem = api}
                        TaskDev(apiPara, jobName, loadSysName, sys)

                let job = Job(node.Job, sys, [devTask])
                //job.UpdateTaskDevPara(node.TaskDevPara)
                job.JobParam <- node.JobParam
                sys.Jobs.Add job |> ignore
                Call.Create(job, parentWrapper)

            | None ->
                if device.AutoGenFromParentSystem || not(node.TaskDevParam.IsDefaultParam)
                then
                    let TaskDevPara =  node.TaskDevParam
                    let autoTaskDev = getAutoGenTaskDev device loadSysName jobName apiName TaskDevPara
                    let job = Job(node.Job, sys, [autoTaskDev])
                    job.JobParam <- node.JobParam
                    sys.Jobs.Add job |> ignore
                    Call.Create(job, parentWrapper)
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
                    ApiName = node.ApiPureName
                    TaskDevParamIO = node.TaskDevParam
                    Parent = parentWrapper
                    PlatformTarget = platformTarget
                    }
                addNewCall callParams

    let createCallVertex (mySys: DsSystem, node: PptNode, parentWrapper: ParentWrapper, platformTarget:PlatformTarget, dicSeg: Dictionary<string, Vertex>) =
        let call =
            if node.IsFunction then
                if node.IsRootNode.Value then
                    Call.Create(getOperatorFunc mySys node, parentWrapper)
                else
                    Call.Create(getCommandFunc mySys node, parentWrapper)
            else
                createCall (mySys, node, parentWrapper, platformTarget)

        node.UpdateCallProperty(call)
        dicSeg.Add(node.Key, call)

    let createAutoPre(mySys: DsSystem, node: PptNode, parentWrapper: ParentWrapper, platformTarget:PlatformTarget, dicAutoPreJob: Dictionary<string, Job>) =
        let param = {
                    MySys = mySys
                    Node = node
                    JobName = node.Job.CombineQuoteOnDemand()
                    DevName = node.DevName
                    ApiName = node.ApiPureName
                    TaskDevParamIO = node.TaskDevParam
                    Parent = parentWrapper
                    PlatformTarget = platformTarget
                    }
        let tasks = HashSet<TaskDev>()
        handleActionJob tasks param
        let job = Job(param.Node.Job, param.MySys, tasks |> Seq.toList)
        match mySys.Jobs.TryFind(fun f -> f.DequotedQualifiedName = job.DequotedQualifiedName) with
        | Some _existingJob -> ()
        | None ->
            mySys.Jobs.Add job

        job.JobParam <- param.Node.JobParam
        dicAutoPreJob.Add(node.Key, job)


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
