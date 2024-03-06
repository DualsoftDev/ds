// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open DocumentFormat.OpenXml.Packaging
open System.Linq
open Dual.Common.Core.FS
open Engine.Parser.FS

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
          
    

    let addLoadedLibSystemNCall
        (
            loadedName,
            apiName,
            mySys: DsSystem,
            parentF: Flow option,
            parentR: Real option,
            node: pptNode
        ) =
        let libFilePath =
            PathManager.getFullPath ($"./{TextLibrary}.ds" |> DsFile) (activeSysDir |> DsDirectory)

        let libRelPath =
            PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)

        let paras loadedName =
            getParams (libFilePath, libRelPath, loadedName, mySys, DuDevice, ShareableSystemRepository())

        let parent =
            match parentR with
            | Some parentR -> DuParentReal parentR
            | None -> DuParentFlow(parentF.Value)

        let addOrGetExistSystem loadedSys loadedName = 
            if not (mySys.LoadedSysExist(loadedName)) then
                mySys.AddLoadedSystem(Device(loadedSys, paras loadedName))
                loadedSys
            else 
                mySys.GetLoadedSys(loadedName).Value.ReferenceSystem


        let apiPureName = GetBracketsRemoveName(apiName).Trim()

        let getLoadedTasks (loadedSys:DsSystem) (newloadedName:string)  =
            let devOrg= addOrGetExistSystem loadedSys newloadedName
            let api = devOrg.ApiItems.First(fun f -> f.Name = apiPureName)
            TaskDev(api, "", "", newloadedName)

        let devOrg, _ = ParserLoader.LoadFromActivePath libFilePath
        if not (devOrg.ApiItems.any (fun f -> f.Name = apiPureName)) then
            node.Shape.ErrorName(ErrID._49, node.PageNum)


        let job =
            let tasks = HashSet<TaskDev>()
            match getJobActionType apiName with
            | MultiAction cnt ->  
                for i in [1..cnt] do
                    let devOrg = if i = 1 then devOrg
                                    else ParserLoader.LoadFromActivePath libFilePath |> fst

                    let mutiName = getDummyDeviceName loadedName i
                    tasks.Add(getLoadedTasks devOrg mutiName)|>ignore
            | _->
                tasks.Add(getLoadedTasks devOrg loadedName)|>ignore
            Job(loadedName + "_" + apiName, tasks |> Seq.toList, None)

        let jobForCall =
            let tempJob = mySys.Jobs.FirstOrDefault(fun f->f.Name = job.Name)
            if tempJob.IsNull()
            then mySys.Jobs.Add(job);job
            else tempJob

        Call.Create(jobForCall, parent)
         