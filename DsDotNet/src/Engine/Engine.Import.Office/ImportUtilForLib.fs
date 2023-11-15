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
            hostIp: string option,
            loadingType,
            sRepo
        ) =
        { ContainerSystem = containerSystem
          AbsoluteFilePath = absoluteFilePath
          RelativeFilePath = PathManager.changeExtension (relativeFilePath.ToFile()) ".ds" //pptx로 부터 .ds로 생성
          LoadedName = loadedName
          ShareableSystemRepository = sRepo

          HostIp = hostIp
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

        let paras =
            getParams (libFilePath, libRelPath, loadedName, mySys, None, DuDevice, ShareableSystemRepository())

        let parent =
            match parentR with
            | Some parentR -> DuParentReal parentR
            | None -> DuParentFlow(parentF.Value)

        let system, loadingPaths = ParserLoader.LoadFromActivePath libFilePath
        let devOrg = system

        if not (devOrg.ApiItems.any (fun f -> f.Name = apiName)) then
            node.Shape.ErrorName(ErrID._49, node.PageNum)

        if not (mySys.LoadedSystems.Select(fun f -> f.Name).Contains(loadedName)) then
            mySys.AddLoadedSystem(Device(devOrg, paras))

        let api = devOrg.ApiItems.First(fun f -> f.Name = apiName)
        let devTask = TaskDev(api, "", "", loadedName) :> DsTask
        let job = Job(loadedName + "_" + apiName, [ devTask ])
        mySys.Jobs.Add(job)
        let call = CallDev.Create(job, parent)

        call.CallTargetJob.DeviceDefs
            .OfType<TaskDev>()
            .Iter(fun a -> a.ApiItem.Xywh <- node.CallPosition)

        call
