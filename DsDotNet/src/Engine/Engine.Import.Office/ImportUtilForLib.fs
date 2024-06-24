namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open DocumentFormat.OpenXml.Packaging
open System.Linq
open System.IO
open Dual.Common.Core.FS
open Engine.Parser.FS
open System.Reflection
open LibraryLoaderModule

[<AutoOpen>]
module ImportUtilForLib =

    type CallParams = {
        MySys: DsSystem
        Node: pptNode
        JobName: string
        ApiName: string
        Parent: ParentWrapper
    }
    with
        member x.WithJobName(newJobName: string) =
            { x with JobName = newJobName }

    let getDeviceOrganization (mySys: DsSystem) (libFilePath: string) (name: string) =
        match mySys.LoadedSystems.TryFind(fun f -> f.Name = name) with
        | Some f -> f.ReferenceSystem
        | None -> ParserLoader.LoadFromActivePath libFilePath Util.runtimeTarget |> fst

    let processSingleTask (tasks: HashSet<TaskDev>) (param: CallParams) (devOrg: DsSystem) (loadedName: string) (apiPureName: string) (devParams: DeviceLoadParameters) =
        tasks.Add(getLoadedTasks param.MySys devOrg loadedName apiPureName devParams param.Node param.Node.JobName) |> ignore

    let getTaskDev (autoGenSys: LoadedSystem option) (loadedName: string) (jobName: string) (apiName: string) =
        match autoGenSys with
        | Some autoGenSys -> getAutoGenDevTask autoGenSys loadedName jobName apiName |> Some
        | None -> None

    let addSingleTask (tasks: HashSet<TaskDev>) (task: TaskDev option) =
        match task with
        | Some t ->
            t.InAddress <- ""
            t.OutAddress <- ""
            tasks.Add(t) |> ignore
        | None -> ()

    let getLibraryPathsAndParams (param: CallParams) =
        let libFilePath, autoGenSys = getNewDevice param.MySys param.JobName param.ApiName
        let relPath = PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)
        let getDevParams name = getParams (libFilePath, relPath, name, param.MySys, DuDevice, ShareableSystemRepository())
        (libFilePath, autoGenSys, getDevParams)

    let processTask (tasks: HashSet<TaskDev>) (param: CallParams) (loadedName: string) (libFilePath: string) (autoGenSys: LoadedSystem option) (getDevParams: string -> DeviceLoadParameters) =
        let devOrg = getDeviceOrganization param.MySys libFilePath loadedName
        let devParams = getDevParams loadedName
        let task = getTaskDev autoGenSys loadedName param.Node.JobName param.ApiName
        addSingleTask tasks task
        if task.IsNone then
            processSingleTask tasks param devOrg loadedName param.ApiName devParams

    let handleSingleJob (tasks: HashSet<TaskDev>) (param: CallParams) =
        let libFilePath, autoGenSys, getDevParams = getLibraryPathsAndParams param
        processTask tasks param param.JobName libFilePath autoGenSys getDevParams

    let handleMultiActionJob (tasks: HashSet<TaskDev>) (param: CallParams) =
        for devIdx in 1 .. param.Node.JobParam.DeviceCount do
            let devName = getMultiDeviceName param.JobName devIdx
            let libFilePath, autoGenSys, getDevParams = getLibraryPathsAndParams (param.WithJobName(devName))
            processTask tasks param devName libFilePath autoGenSys getDevParams

    let addNewCall (param: CallParams) =
        let jobName = param.Node.JobName
        let apiPureName = GetBracketsRemoveName(param.ApiName).Trim()
        let tasks = HashSet<TaskDev>()

        match param.Node.JobParam.JobMulti with
        | Single -> handleSingleJob tasks param
        | MultiAction (_, _, _, _) -> handleMultiActionJob tasks param

        let job = Job(jobName, param.MySys, tasks |> Seq.toList)
        job.UpdateJobParam(param.Node.JobParam)

        let jobForCall =
            match param.MySys.Jobs.TryFind(fun f -> f.Name = job.Name) with
            | None -> param.MySys.Jobs.Add(job); job
            | Some existingJob -> existingJob

        Call.Create(jobForCall, param.Parent)
