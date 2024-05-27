namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open PPTObjectModule
open Dual.Common.Core.FS
open Engine.Core
open System
open System.IO
open System.Data
open LibraryLoaderModule
open System.Reflection

[<AutoOpen>]
module ImportUtilVertex =

    let getJobName (node: pptNode) apiName (mySys: DsSystem) =
        let jobFirstName = node.CallName + "_" + apiName
        match mySys.Jobs |> Seq.tryFind (fun job -> job.Name = jobFirstName) with
        | Some job -> node.JobName
        | None -> jobFirstName

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

    let getCallFromLoadedSys (sys: DsSystem) (node: pptNode) (loadSysName: string) (apiName: string) parentWrapper =
        match sys.Jobs |> Seq.tryFind (fun job ->
            job.DeviceDefs |> Seq.exists (fun d -> d.DeviceName = loadSysName && d.ApiName = apiName)) with
        | Some job -> Call.Create(job, parentWrapper)
        | None ->
            let device = sys.Devices |> Seq.find (fun d -> d.Name = loadSysName)
            let api = device.ReferenceSystem.ApiItems |> Seq.find (fun a -> a.Name = apiName)
            let devTask = TaskDev(api, node.JobName, node.DevParamIn, node.DevParamOut, loadSysName)
            let job = Job(node.JobName, sys, [devTask])
            sys.Jobs.Add job |> ignore
            Call.Create(job, parentWrapper)

    let getCallFromMultiLoadedSys (sys: DsSystem) (node: pptNode) (loadSysName: string) (apiName: string) parentWrapper =

            let multiName = getMultiDeviceName loadSysName node.JobType.Value.DeviceCount
            match sys.Jobs |> Seq.tryFind (fun job ->
                job.DeviceDefs |> Seq.exists (fun d -> d.DeviceName = multiName && d.ApiName = apiName)) with
            | Some job -> Call.Create(job, parentWrapper)
            | None ->
                let devTasks =
                    seq{
                        for i in [1..node.JobType.Value.DeviceCount] do
                            let multiName = getMultiDeviceName loadSysName i 
                            let device = sys.Devices |> Seq.find (fun d -> d.Name = multiName)
                            let api = device.ReferenceSystem.ApiItems |> Seq.find (fun a -> a.Name = apiName)
                            yield  TaskDev(api, node.JobName, node.DevParamIn, node.DevParamOut, multiName)
                    }

                let job = Job(node.JobName, sys, devTasks)
                sys.Jobs.Add job |> ignore
                Call.Create(job, parentWrapper)

    let getCallFromNewSys (sys: DsSystem) (node: pptNode) jobName parentWrapper =
        let apiName = node.CallApiName
        let loadedName = node.CallName

        let apiNameForLib = GetBracketsRemoveName(apiName).Trim()
        let libAbsolutePath, autoGenSys = getLibraryPath sys loadedName apiNameForLib

        let autoGenDevTask =
            match autoGenSys with
            | Some autoGenSys ->
                let referenceSystem = autoGenSys.ReferenceSystem
                let defaultParams = TextAddrEmpty |> defaultDevParam, TextAddrEmpty |> defaultDevParam
                Some (createTaskDevUsingApiName referenceSystem jobName loadedName apiName defaultParams)
            | None -> None

        addLibraryNCall (libAbsolutePath, loadedName, apiName, sys, parentWrapper, node, autoGenDevTask)

    let createCallVertex (mySys: DsSystem, node: pptNode, parentWrapper: ParentWrapper, dicSeg: Dictionary<string, Vertex>) =
        let sysName, apiName = GetSysNApi(node.PageTitle, node.Name)
        let jobName = getJobName node apiName mySys
        let loadedSys = mySys.Devices.Select(fun d -> d.Name)
        let call =
            if node.IsFunction then
                if node.IsRootNode.Value then
                    Call.Create(getOperatorFunc mySys node, parentWrapper)
                else
                    Call.Create(getCommandFunc mySys node, parentWrapper)
            else
                    
                let multiName = getMultiDeviceName sysName (node.JobType.Value.DeviceCount)
                if loadedSys |> Seq.contains sysName 
                then
                    getCallFromLoadedSys mySys node sysName apiName parentWrapper
                elif loadedSys |> Seq.contains multiName 
                then
                    getCallFromMultiLoadedSys mySys node sysName apiName parentWrapper
                else
                    getCallFromNewSys mySys node jobName parentWrapper

        node.UpdateCallProperty(call)
        dicSeg.Add(node.Key, call)

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
