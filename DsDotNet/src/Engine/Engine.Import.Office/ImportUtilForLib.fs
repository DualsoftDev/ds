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


        let devOrg, _ = ParserLoader.LoadFromActivePath libFilePath
        let apiPureName = GetBracketsRemoveName(apiName)
        if not (devOrg.ApiItems.any (fun f -> f.Name = apiPureName)) then
            node.Shape.ErrorName(ErrID._49, node.PageNum)

        let tasks = HashSet<DsTask>()
        let jobType = getJobActionType apiName
        match jobType with
        | MultiAction cnt ->  
            for i in [1..cnt] do
                let devOrg = if i = 1 then devOrg
                             else ParserLoader.LoadFromActivePath libFilePath |> fst

                let mutiName = $"{loadedName}[{cnt}]{i}"
                let devOrg= addOrGetExistSystem devOrg mutiName
                let api = devOrg.ApiItems.First(fun f -> f.Name = apiPureName)
                tasks.Add (TaskDev(api, "", "", mutiName) :> DsTask)  |>ignore

        | _->
            let devOrg= addOrGetExistSystem devOrg loadedName
            let api = devOrg.ApiItems.First(fun f -> f.Name = apiPureName)
            tasks.Add (TaskDev(api, "", "", loadedName) :> DsTask)  |>ignore


        let job = Job(loadedName + "_" + apiName, tasks |> Seq.toList)
        mySys.Jobs.Add(job)

        let call = Call.Create(job, parent)
        call.TargetJob.DeviceDefs
            .OfType<TaskDev>()
            .Iter(fun a -> a.ApiItem.Xywh <- node.CallPosition)

        call
