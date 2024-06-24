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

    let getDeviceOrganization (mySys: DsSystem) (libFilePath: string) (name: string) =
        match mySys.LoadedSystems.TryFind(fun f -> f.Name = name) with
        | Some f -> f.ReferenceSystem
        | None -> ParserLoader.LoadFromActivePath libFilePath Util.runtimeTarget |> fst

    let processSingleTask (tasks: HashSet<TaskDev>) (mySys: DsSystem) (devOrg: DsSystem) (loadedName: string) (apiPureName: string) (devParams: DeviceLoadParameters) (node: pptNode) (jobName: string) =
        tasks.Add(getLoadedTasks mySys devOrg loadedName apiPureName devParams node jobName) |> ignore

    let getTaskDev (autoGenSys: LoadedSystem option) (loadedName: string) (jobName: string) (apiName: string) =
        match autoGenSys with
        | Some autoGenSys -> getAutoGenDevTask autoGenSys loadedName jobName apiName |> Some
        | None -> None

    let addSingleJobTask (tasks: HashSet<TaskDev>) (task: TaskDev option) =
        match task with
        | Some t ->
            t.InAddress <- ""
            t.OutAddress <- ""
            tasks.Add(t) |> ignore
        | None -> ()

    let getLibraryPathsAndParams (mySys: DsSystem) (loadedName: string) (apiNameForLib: string) =
        let libFilePath, autoGenSys = getNewDevice mySys loadedName apiNameForLib
        let libRelPath = PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)
        let getDevParams name = getParams (libFilePath, libRelPath, name, mySys, DuDevice, ShareableSystemRepository())
        (libFilePath, autoGenSys, getDevParams)

    let processTask (tasks: HashSet<TaskDev>) (mySys: DsSystem) (loadedName: string) (libFilePath: string) (autoGenSys: LoadedSystem option) (getDevParams: string -> DeviceLoadParameters) (apiPureName: string) (node: pptNode) =
        let devOrg = getDeviceOrganization mySys libFilePath loadedName
        let devParams = getDevParams loadedName
        let task = getTaskDev autoGenSys loadedName node.JobName apiPureName
        addSingleJobTask tasks task
        if task.IsNone then
            processSingleTask tasks mySys devOrg loadedName apiPureName devParams node node.JobName

    let handleSingleJob (tasks: HashSet<TaskDev>) (mySys: DsSystem) (loadedName: string) (apiPureName: string) (node: pptNode) =
        let libFilePath, autoGenSys, getDevParams = getLibraryPathsAndParams mySys loadedName apiPureName
        processTask tasks mySys loadedName libFilePath autoGenSys getDevParams apiPureName node

    let handleMultiActionJob (tasks: HashSet<TaskDev>) (mySys: DsSystem) (loadedName: string) (apiPureName: string) (node: pptNode) =
        for devIndex in 1 .. node.JobParam.DeviceCount do
            let devName = getMultiDeviceName loadedName devIndex
            let libFilePath, autoGenSys, getDevParams = getLibraryPathsAndParams mySys devName apiPureName
            processTask tasks mySys devName libFilePath autoGenSys getDevParams apiPureName node

    let addNewCall (loadedName: string, apiName: string, mySys: DsSystem, parent: ParentWrapper, node: pptNode) =
        let jobName = node.JobName
        let apiNameForLib = GetBracketsRemoveName(apiName).Trim()
        let apiPureName = GetBracketsRemoveName(apiName).Trim()
        let tasks = HashSet<TaskDev>()

        match node.JobParam.JobMulti with
        | Single -> handleSingleJob tasks mySys loadedName apiPureName node
        | MultiAction (_, _, _, _) -> handleMultiActionJob tasks mySys loadedName apiPureName node

        let job = Job(jobName, mySys, tasks |> Seq.toList)
        job.UpdateJobParam(node.JobParam)

        let jobForCall =
            match mySys.Jobs.TryFind(fun f -> f.Name = job.Name) with
            | None -> mySys.Jobs.Add(job); job
            | Some existingJob -> existingJob

        Call.Create(jobForCall, parent)
