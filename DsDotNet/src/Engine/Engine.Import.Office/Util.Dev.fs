// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open Engine.Core
open System.IO
open System.Linq
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open System.Reflection
open LibraryLoaderModule

[<AutoOpen>]
module ImportUtilForDev =



    type DevInfo = { DevName: string; ApiName: string }

    let getParams (
        absoluteFilePath: string,
        relativeFilePath: string,
        loadedName: string,
        containerSystem: DsSystem,
        loadingType,
        sRepo )
      =
        {   ContainerSystem = containerSystem
            AbsoluteFilePath = absoluteFilePath
            RelativeFilePath = PathManager.changeExtension (relativeFilePath.ToFile()) "ds" //pptx로 부터 .ds로 생성
            LoadedName = loadedName
            ShareableSystemRepository = sRepo
            LoadingType = loadingType }


    let createAutoGenDev(loadedName, (mySys: DsSystem)) =
        match mySys.LoadedSystems.TryFind(fun f->f.Name = loadedName) with
        | Some autoGenDev ->  autoGenDev
        | None ->
            let autoGenFile = $"./dsLib/AutoGen/{loadedName}.ds"
            let autoSys = DsSystem.Create("autoSys")
            let libFilePath =
                PathManager.getFullPath (DsFile autoGenFile) (DsDirectory activeSysDir)
            let libRelPath =
                PathManager.getRelativePath (DsFile currentFileName) (DsFile libFilePath)

            let paras (loadedName) =
                getParams (libFilePath, libRelPath, loadedName, mySys, DuDevice, ShareableSystemRepository())
            let dev = Device(autoSys, paras loadedName, true)
            mySys.AddLoadedSystem(dev)
            dev

    let getLibraryInfos()=
        let runDir = Assembly.GetEntryAssembly().Location |> Path.GetDirectoryName
        let runDir =
            if Net48Path.Exists (Path.Combine(runDir, "dsLib")) then
                runDir
            else
                @$"{__SOURCE_DIRECTORY__}../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils"

        let curDir = currentFileName |> Path.GetDirectoryName

        let libConfigPath = Path.Combine(runDir, "dsLib", "Library.config")

        let libPath =
            if not <| Net48Path.Exists libConfigPath then
                failWithLog $"{libConfigPath} file not found"

            libConfigPath

        let libConfig = LoadLibraryConfig(libPath)
        libConfig.LibraryInfos, runDir

    // Call Graph
    // {
    //      loadSystem > BuildSystem > MakeSegment > createCallVertex > createCall > addNewCall,
    //                               > MakeSegment > createAutoPre
    // }
    //  > handleActionJob > getLibraryPathsAndParams > getNewDevice
    let getNewDevice (mySys:DsSystem) loadedName apiName =
        let curDir = currentFileName  |> Path.GetDirectoryName
        let libraryInfos, runDir = getLibraryInfos()

        if libraryInfos.ContainsKey(apiName) then
            let libPath =  libraryInfos.[apiName]

            let libAbsolutePath = Path.Combine(curDir, libPath)
            let curLibDir = Path.GetDirectoryName libAbsolutePath
            if not (Copylibrary.Contains(libAbsolutePath)) then  //시스템 라이브러리는 한번 덮어쓰기
                if not (Directory.Exists curLibDir) then
                    Directory.CreateDirectory curLibDir |> ignore

                let sourcePath = PathManager.combineFullPathFile([|runDir; libPath|])
                let libAbsolutePath = PathManager.combineFullPathFile([|libAbsolutePath|])
                if sourcePath <> libAbsolutePath
                then
                    File.Copy(sourcePath, libAbsolutePath, true)

                Copylibrary.Add libAbsolutePath |> ignore

            libAbsolutePath, None
        else
            let autoGenFile = $"./dsLib/AutoGen/{loadedName}.ds"
            let autoGenAbsolutePath =
                PathManager.getFullPath (autoGenFile |> DsFile) (activeSysDir |> DsDirectory)

            match mySys.LoadedSystems.TryFind(fun f->f.Name = loadedName) with
            | Some autoGenDev ->  autoGenAbsolutePath, Some autoGenDev
            | None ->
                let newLoadedDev = createAutoGenDev (loadedName, mySys)
                autoGenAbsolutePath, Some newLoadedDev

    let addOrGetExistSystem (mySys:DsSystem) loadedSys (loadedName:string) (taskDevParamIO:DeviceLoadParameters) =
        if not (mySys.LoadedSysExist(loadedName)) then
            mySys.AddLoadedSystem(Device(loadedSys, taskDevParamIO, false))

        loadedSys

    let getAutoGenTaskDev (autoGenSys:LoadedSystem) (loadedName:string) (apiName:string) =
        let referenceSystem = autoGenSys.ReferenceSystem
        referenceSystem.CreateTaskDev(loadedName, apiName)

    let getLoadedTasks (mySys:DsSystem)(loadedSys:DsSystem) (newloadedName:string) (apiName:string) (loadParameters:DeviceLoadParameters) (node:PptNode) jobName =
        let tastDevKey = $"{newloadedName}_{apiName}"
        //let taskDevParam = node.TaskDevParam
        let jobFqdn = node.Job.Combine()

        let devCalls = mySys.GetTaskDevsCall().DistinctBy(fun (td, c) -> (td, c.TargetJob))

        match devCalls.TryFind(fun (d,c) -> d.FullName = tastDevKey) with
        | Some (taskDev, c) ->
            let api = loadedSys.ApiItems.First(fun f -> f.Name = apiName)
            //if not (taskDevParam.IsDefaultParam) then
            //    taskDev.AddOrUpdateApiTaskDevParam(jobFqdn, api, taskDevParam)
            taskDev
        | None ->
            let devOrg = addOrGetExistSystem mySys loadedSys newloadedName loadParameters
            match devOrg.ApiItems.TryFind(fun f -> f.Name = apiName) with
            | Some api ->
                //let apiParam = {TaskDevParamIO =  taskDevParam; ApiItem = api}
                mySys.CreateTaskDev(newloadedName, api)
            | None ->
                failWithLog $"Api {apiName} not found in {newloadedName}"
