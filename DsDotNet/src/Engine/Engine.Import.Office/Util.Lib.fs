namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open Dual.Common.Core.FS
open Engine.Parser.FS

[<AutoOpen>]
module ImportUtilForLib =

    type CallParams = {
        MySys: DsSystem
        Node: PptNode
        JobName: string
        DevName: string
        ApiName: string
        ValueParamIO: ValueParamIO
        Parent: ParentWrapper
        HwCPU:HwCPU
    }
    with
        member x.WithDevName(newDevName: string) =
            { x with DevName = newDevName }

    let getDeviceOrganization (mySys: DsSystem) (libFilePath: string) (name: string) (hwCPU:HwCPU) =
        match mySys.LoadedSystems.TryFind(fun f -> f.Name = name) with
        | Some f -> f.ReferenceSystem
        | None -> (ParserLoader.LoadFromDevicePath libFilePath name hwCPU) |>fst

    let processSingleTask (tasks: HashSet<TaskDev>) (param: CallParams) (devOrg: DsSystem) (loadedName: string) (apiName: string) (taskDevParamIO: DeviceLoadParameters) =
        let task = getLoadedTasks param.MySys devOrg loadedName apiName taskDevParamIO param.Node (param.Node.Job.Combine())
        tasks.Add(task) |> ignore

    let getTaskDev (autoGenSys: LoadedSystem option) (loadedName: string)  (apiName: string) =
        match autoGenSys with
        | Some autoGenSys -> getAutoGenTaskDev autoGenSys loadedName  apiName |> Some
        | None -> None

    let addSingleTask (tasks: HashSet<TaskDev>) (task: TaskDev option) =
        match task with
        | Some t ->
            t.InAddress <- ""
            t.OutAddress <- ""
            tasks.Add(t) |> ignore
        | None -> ()

    let getLibraryPathsAndParams (param: CallParams) =
        let libFilePath, autoGenSys = getNewDevice param.MySys param.DevName param.ApiName
        let relPath = PathManager.getRelativePath (DsFile currentFileName) (DsFile libFilePath)
        let getProperty name = getParams (libFilePath, relPath, name, param.MySys, DuDevice, ShareableSystemRepository())
        (libFilePath, autoGenSys, getProperty)

    let processTask (tasks: HashSet<TaskDev>) (param: CallParams) (loadedName: string) (libFilePath: string) (autoGenSys: LoadedSystem option) (getProperty: string -> DeviceLoadParameters) =
        let devOrg = getDeviceOrganization param.MySys libFilePath loadedName (param.HwCPU)
        let TaskDevParams = getProperty loadedName

        let task = getTaskDev autoGenSys loadedName  param.ApiName
        addSingleTask tasks task
        if task.IsNone then
            processSingleTask tasks param devOrg loadedName param.ApiName TaskDevParams


    let handleActionJob (tasks: HashSet<TaskDev>) (param: CallParams) =

        for devIdx in 1 .. param.Node.JobParam.TaskDevCount do
            let devName =
                if param.Node.JobParam.TaskDevCount = 1
                then
                    param.Node.DevName
                else
                    getMultiDeviceName param.Node.DevName devIdx

            let libFilePath, autoGenSys, getProperty = getLibraryPathsAndParams (param.WithDevName(devName))
            processTask tasks param devName libFilePath autoGenSys getProperty

    let addNewCall (param: CallParams) =
        let jobName = param.Node.Job.CombineDequoteOnDemand()
        let tasks = HashSet<TaskDev>()

        handleActionJob tasks param

        let jobForCall =
            match param.MySys.Jobs.TryFind(fun f -> f.DequotedQualifiedName = jobName) with
            | Some existingJob -> existingJob
            | None ->
                let job = Job(param.Node.Job, param.MySys, tasks |> Seq.toList)
                updateAddressSkip(param.Node.JobParam, job)
                param.MySys.Jobs.Add(job)
                job

        let call = param.Parent.CreateCall(jobForCall, param.ValueParamIO)
        call.Name <- param.Node.Name
        call
