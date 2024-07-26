// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open Engine.Core
open System.IO
open System.Linq
open Dual.Common.Core.FS
open System.Reflection
open LibraryLoaderModule

[<AutoOpen>]
module ImportUtilForDev =



    type DevInfo = { DevName: string; ApiName: string }
    
    let getParams
        (
            absoluteFilePath: string,
            relativeFilePath: string,
            loadedName: string,
            containerSystem: DsSystem,
            loadingType,
            sRepo
        ) =
        { ContainerSystem = containerSystem
          AbsoluteFilePath = absoluteFilePath
          RelativeFilePath = PathManager.changeExtension (relativeFilePath.ToFile()) ".ds" //pptx로 부터 .ds로 생성
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
                PathManager.getFullPath (autoGenFile |> DsFile) (activeSysDir |> DsDirectory)
            let libRelPath =
                PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)

            let paras (loadedName) =
                getParams (libFilePath, libRelPath, loadedName, mySys, DuDevice, ShareableSystemRepository())
            let dev = Device(autoSys, paras loadedName, true)
            mySys.AddLoadedSystem(dev)
            dev

    let getLibraryInfos()= 
        let runDir = Assembly.GetEntryAssembly().Location |> Path.GetDirectoryName
        let runDir  = if Path.Exists (Path.Combine(runDir, "dsLib"))
                        then runDir
                        else @$"{__SOURCE_DIRECTORY__}../../../../bin/net7.0-windows/"      
                     
        let curDir = currentFileName  |> Path.GetDirectoryName

        let libConfigPath = Path.Combine(runDir, "dsLib", "Library.config")

        let libPath  = if Path.Exists libConfigPath      
                       then libConfigPath
                       else failWithLog $"{libConfigPath}Library.config file not found"


        let libConfig = LoadLibraryConfig(libPath)
        libConfig.LibraryInfos, runDir

    let getNewDevice (mySys:DsSystem) loadedName apiName =
        let curDir = currentFileName  |> Path.GetDirectoryName
        let LibraryInfos, runDir = getLibraryInfos()
        if LibraryInfos.ContainsKey(apiName) 
        then
            let libPath =  LibraryInfos.[apiName]
                
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

    let addOrGetExistSystem (mySys:DsSystem)  loadedSys loadedName (taskDevParaIO:DeviceLoadParameters) = 
        if not (mySys.LoadedSysExist(loadedName)) then
            mySys.AddLoadedSystem(Device(loadedSys, taskDevParaIO, false))
            loadedSys
        else 
            loadedSys

    let getAutoGenTaskDev  (autoGenSys:LoadedSystem) loadedName jobName apiName (taskDevParaIO:TaskDevParaIO)= 
        let referenceSystem = autoGenSys.ReferenceSystem
        createTaskDevUsingApiName referenceSystem jobName loadedName apiName  taskDevParaIO

    let getLoadedTasks (mySys:DsSystem)(loadedSys:DsSystem) (newloadedName:string) (apiPureName:string) (loadParameters:DeviceLoadParameters) (node:pptNode) jobName =
        let tastDevKey = $"{newloadedName}_{apiPureName}"
        let taskDevParam =  node.TaskDevPara 

        let devCalls =  mySys.GetTaskDevsCall().DistinctBy(fun (td, c) -> (td, c.TargetJob))

        match devCalls.TryFind(fun (d,c) -> d.GetApiStgName(c.TargetJob) = tastDevKey) with
        | Some (taskDev, c) -> 
                    let api = loadedSys.ApiItems.First(fun f -> f.Name = apiPureName)
                    if not(taskDevParam.IsDefaultParam)
                    then
                        taskDev.AddOrUpdateApiTaskDevPara(c.TargetJob, api, taskDevParam)
                    taskDev 
        | None ->
            let devOrg = addOrGetExistSystem mySys loadedSys newloadedName loadParameters
            match devOrg.ApiItems.TryFind(fun f -> f.Name = apiPureName) with
            | Some api -> 
                        let apiPara  = {TaskDevParaIO =  taskDevParam; ApiItem = api}
                        TaskDev(apiPara, jobName, newloadedName, mySys)
            | None ->
                failWithLog $"Api {apiPureName} not found in {newloadedName}"
                    