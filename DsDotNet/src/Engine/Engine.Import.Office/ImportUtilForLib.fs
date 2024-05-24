// Copyright (c) Dual Inc.  All Rights Reserved.
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
            let autoSys = DsSystem("autoSys")
            let libFilePath =
                PathManager.getFullPath (autoGenFile |> DsFile) (activeSysDir |> DsDirectory)
            let libRelPath =
                PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)

            let paras (loadedName) =
                getParams (libFilePath, libRelPath, loadedName, mySys, DuDevice, ShareableSystemRepository())
            let dev = Device(autoSys, paras loadedName, true)
            mySys.AddLoadedSystem(dev)
            dev


    let getLibraryPath (mySys:DsSystem) loadedName apiName =
        let runDir = Assembly.GetEntryAssembly().Location |> Path.GetDirectoryName
        let curDir = currentFileName  |> Path.GetDirectoryName

        let libraryConfigFileName = "Library.config"
        let libConfigPath = Path.Combine(runDir, "dsLib", libraryConfigFileName)

        let libPath  = if Path.Exists libConfigPath      
                       then libConfigPath
                       else Path.Combine(curDir, "dsLib", libraryConfigFileName)


        let libConfig = LoadLibraryConfig(libPath)
        if libConfig.LibraryInfos.ContainsKey(apiName) 
        then
            let libPath =  libConfig.LibraryInfos.[apiName]
                
            let libAbsolutePath = Path.Combine(curDir, libPath)
            let curLibDir = Path.GetDirectoryName libAbsolutePath
            if not (File.Exists libAbsolutePath) then
                if not (Directory.Exists curLibDir) then
                    Directory.CreateDirectory curLibDir |> ignore

                let sourcePath = Path.Combine(runDir, libPath)
                File.Copy(sourcePath, libAbsolutePath)

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


    let addLibraryNCall
        (
            libFilePath,
            loadedName,
            apiName,
            mySys: DsSystem,
            parentWrapper:ParentWrapper,
            node: pptNode,
            autoTaskDev: TaskDev option,
            jobName: string
        ) =

        let parent =parentWrapper

        let job =
            if autoTaskDev.IsSome
            then
                let tasks = HashSet<TaskDev>()
                autoTaskDev.Value.InAddress <- ("")
                autoTaskDev.Value.OutAddress<- ("")
                tasks.Add(autoTaskDev.Value)|>ignore
                Job(jobName, mySys, tasks |> Seq.toList)
            else 
                let libRelPath =
                    PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)

                let paras loadedName =
                    getParams (libFilePath, libRelPath, loadedName, mySys, DuDevice, ShareableSystemRepository())

                let addOrGetExistSystem loadedSys loadedName = 
                    if not (mySys.LoadedSysExist(loadedName)) then
                        mySys.AddLoadedSystem(Device(loadedSys, paras loadedName, false))
                        loadedSys
                    else 
                        mySys.GetLoadedSys(loadedName).Value.ReferenceSystem


                let apiPureName = GetBracketsRemoveName(apiName).Trim()
                let getLoadedTasks (loadedSys:DsSystem) (newloadedName:string)  =
                    let devOrg= addOrGetExistSystem loadedSys newloadedName
                    let api = devOrg.ApiItems.First(fun f -> f.Name = apiPureName)



                    if node.DevParam.IsSome then
                        let inParam  = match node.DevParam.Value |>fst
                                        with
                                            | Some p -> p
                                            | _ -> ""|>defaultDevParam
                        let outParam  = match node.DevParam.Value |>snd
                                        with
                                            | Some p -> p
                                            | _ -> ""|>defaultDevParam
                                            
                        TaskDev(api,jobName,  inParam, outParam, newloadedName)
                    else 
                        TaskDev(api,jobName,  ""|>defaultDevParam, ""|>defaultDevParam, newloadedName)

                    
                let devOrg, _ = ParserLoader.LoadFromActivePath libFilePath Util.runtimeTarget
                if not (devOrg.ApiItems.any (fun f -> f.Name = apiPureName)) then
                    node.Shape.ErrorName(ErrID._49, node.PageNum)

                let tasks = HashSet<TaskDev>()
                match getJobActionType apiName with
                | MultiAction (_, cnt) ->  
                    for i in [1..cnt] do
                        let devOrg = if i = 1 then devOrg
                                        else ParserLoader.LoadFromActivePath libFilePath Util.runtimeTarget |> fst

                        let mutiName = getDummyDeviceName loadedName i
                        tasks.Add(getLoadedTasks devOrg mutiName)|>ignore
                | _->
                    tasks.Add(getLoadedTasks devOrg loadedName)|>ignore
                Job(jobName, mySys, tasks |> Seq.toList)

        let jobForCall =
            let tempJob = mySys.Jobs.FirstOrDefault(fun f->f.Name = job.Name)
            if tempJob.IsNull()
            then mySys.Jobs.Add(job);job
            else tempJob
        
        Call.Create(jobForCall, parent)

        //if node.NodeType = CALLOPFunc
        //then
        //    let func = OperatorFunction.Create(node.OperatorName, "") 
        //    mySys.Functions.Add (func:> Func) |> ignore
        //    Call.Create(func, parent)
        //else 
        //    Call.Create(jobForCall, parent)

                
        
         