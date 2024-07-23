namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open PPTConnectionModule
open Dual.Common.Core.FS
open Engine.Core
open System
open System.IO
open System.Data
open LibraryLoaderModule
open System.Reflection
open Engine.Parser.FS

[<AutoOpen>]
module ImportUtilVertex =


    let getOperatorFunc (sys: DsSystem) (node: pptNode) =
        sys.Functions
        |> Seq.tryFind (fun f -> f.Name = node.OperatorName)
        |> Option.defaultWith (fun () ->
            let newFunc = OperatorFunction(node.OperatorName) :> Func
            sys.Functions.Add newFunc |> ignore
            newFunc
        )

    let getCommandFunc (sys: DsSystem) (node: pptNode) =
        sys.Functions
        |> Seq.tryFind (fun f -> f.Name = node.CommandName)
        |> Option.defaultWith (fun () ->
            let newFunc = CommandFunction(node.CommandName) :> Func
            sys.Functions.Add newFunc |> ignore
            newFunc
        )

    let getCallFromLoadedSys (sys: DsSystem) (device: LoadedSystem) (node: pptNode) (apiName: string) parentWrapper =
        let loadSysName = device.Name
        let jobName = node.Job.Combine()


        match sys.Jobs |> Seq.tryFind (fun job -> job.UnqualifiedName = jobName) with
        | Some job -> Call.Create(job, parentWrapper)
        | None ->
            match device.ReferenceSystem.ApiItems |> Seq.tryFind (fun a -> a.PureName = node.ApiPureName) with
            |Some api ->
                let devTask = 
                    let TaskDevPara =  node.TaskDevPara 

                    match sys.TaskDevs.TryFind(fun d->d.ApiItems.Contains(api)) with 
                    | Some (taskDev) ->
                        taskDev.AddOrUpdateApiTaskDevPara(jobName, api, TaskDevPara)
                        taskDev
                    | _ -> 
                        let apiPara  = {TaskDevParaIO =  TaskDevPara; ApiItem = api}
                        TaskDev(apiPara, jobName, loadSysName, sys)

                let job = Job(node.Job, sys, [devTask])
                //job.UpdateTaskDevPara(node.TaskDevPara)
                job.UpdateJobParam(node.JobParam)
                sys.Jobs.Add job |> ignore
                Call.Create(job, parentWrapper)

            | None -> 
                if device.AutoGenFromParentSystem || not(node.TaskDevPara.IsDefaultParam)
                then
                    let TaskDevPara =  node.TaskDevPara 
                    let autoTaskDev = getAutoGenTaskDev device loadSysName jobName apiName TaskDevPara
                    let job = Job(node.Job, sys, [autoTaskDev])
                    job.UpdateJobParam(node.JobParam)
                    sys.Jobs.Add job |> ignore
                    Call.Create(job, parentWrapper)
                else 
                    let ableApis = String.Join(", ", device.ReferenceSystem.ApiItems.Select(fun a->a.Name))
                    failwithlog $"Loading system ({loadSysName}:{device.AbsoluteFilePath}) \r\napi ({apiName}) not found \r\nApi List : {ableApis}"


    let private createCall (mySys: DsSystem, node: pptNode, parentWrapper: ParentWrapper) =
        match  mySys.LoadedSystems.TryFind(fun d -> d.Name = $"{node.DevName}") with
            |  Some dev -> 
                getCallFromLoadedSys mySys dev node node.ApiName parentWrapper
            | _  ->
                let callParams = {
                    MySys = mySys
                    Node = node
                    JobName = node.Job.CombineQuoteOnDemand()
                    DevName = node.DevName
                    ApiName = node.ApiPureName
                    TaskDevParaIO = node.TaskDevPara
                    Parent = parentWrapper
                    }
                addNewCall callParams
   
    let createCallVertex (mySys: DsSystem, node: pptNode, parentWrapper: ParentWrapper, dicSeg: Dictionary<string, Vertex>) =
        let call =
            if node.IsFunction then
                if node.IsRootNode.Value then
                    Call.Create(getOperatorFunc mySys node, parentWrapper)
                else
                    Call.Create(getCommandFunc mySys node, parentWrapper)
            else
                createCall (mySys, node, parentWrapper)

        node.UpdateCallProperty(call)
        dicSeg.Add(node.Key, call)

    let createAutoPre(mySys: DsSystem, node: pptNode, parentWrapper: ParentWrapper, dicSeg: Dictionary<string, Vertex>) =
        let autoPre =  createCall (mySys, node, parentWrapper)
        dicSeg.Add(node.Key, autoPre)

    let  getParent
        (
            edge: pptEdge,
            parents: Dictionary<pptNode, seq<pptNode>>,
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


    let getOtherFlowReal (flows: Flow seq, nodeEx: pptNode) =
        let flowName, nodeName = nodeEx.Name.Split('.')[0], nodeEx.Name.Split('.')[1]

        match flows.TryFind(fun f -> f.Name = flowName) with
        | Some flow ->
            match flow.Graph.Vertices.TryFind(fun f -> f.Name = nodeName) with
            | Some real -> real
            | None -> nodeEx.Shape.ErrorName($"{ErrID._27} Error Name : [{nodeName}]", nodeEx.PageNum)
        | None -> nodeEx.Shape.ErrorName($"{ErrID._26} Error Name : [{flowName}]", nodeEx.PageNum)
