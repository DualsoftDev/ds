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
        DevName: string
        ApiName: string
        TaskDevParaIO: TaskDevParaIO
        Parent: ParentWrapper
    }
    with
        member x.WithDevName(newDevName: string) =
            { x with DevName = newDevName }

    let getDeviceOrganization (mySys: DsSystem) (libFilePath: string) (name: string) =
        match mySys.LoadedSystems.TryFind(fun f -> f.Name = name) with
        | Some f -> f.ReferenceSystem
        | None -> ParserLoader.LoadFromDevicePath libFilePath Util.runtimeTarget |> fst

    let processSingleTask (tasks: HashSet<TaskDev>) (param: CallParams) (devOrg: DsSystem) (loadedName: string) (apiPureName: string) (taskDevParaIO: DeviceLoadParameters) =
        tasks.Add(getLoadedTasks param.MySys devOrg loadedName apiPureName taskDevParaIO param.Node (param.Node.Job.Combine())) |> ignore

    let getTaskDev (autoGenSys: LoadedSystem option) (loadedName: string) (jobName: string) (apiName: string)  (taskDevParaIO:TaskDevParaIO)=
        match autoGenSys with
        | Some autoGenSys -> getAutoGenTaskDev autoGenSys loadedName jobName apiName taskDevParaIO|> Some
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
        let relPath = PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)
        let getProperty name = getParams (libFilePath, relPath, name, param.MySys, DuDevice, ShareableSystemRepository())
        (libFilePath, autoGenSys, getProperty)

    let processTask (tasks: HashSet<TaskDev>) (param: CallParams) (loadedName: string) (libFilePath: string) (autoGenSys: LoadedSystem option) (getProperty: string -> DeviceLoadParameters) =
        let devOrg = getDeviceOrganization param.MySys libFilePath loadedName
        let TaskDevParas = getProperty loadedName

        let task = getTaskDev autoGenSys loadedName (param.Node.Job.Combine()) param.ApiName  param.TaskDevParaIO
        addSingleTask tasks task
        if task.IsNone then
            processSingleTask tasks param devOrg loadedName param.ApiName TaskDevParas


    let handleMultiActionJob (tasks: HashSet<TaskDev>) (param: CallParams) =
        
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
        let jobName = param.Node.Job.CombineQuoteOnDemand()
        let tasks = HashSet<TaskDev>()

        handleMultiActionJob tasks param

  
        let jobForCall =
            match param.MySys.Jobs.TryFind(fun f -> f.QualifiedName = jobName) with
            | Some existingJob -> existingJob
            | None -> 
                let job = Job(param.Node.Job, param.MySys, tasks |> Seq.toList)
                job.UpdateJobParam(param.Node.JobParam)
                param.MySys.Jobs.Add(job)
                job

        let call = Call.Create(jobForCall, param.Parent)
        call.Name <- param.Node.Job.Combine()
        call
