// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open DocumentFormat.OpenXml.Packaging
open System.Linq
open Dual.Common.Core.FS

[<AutoOpen>]
module ImportUtilForLib =



    type DevInfo = {
        DevName: string
        ApiName: string
    }
    let libDevOrgSys = Dictionary<string, DsSystem>()
    let getParams(directoryName:string , relativeFilePath:string, loadedName:string
                    , containerSystem:DsSystem
                    , hostIp:string option
                    , loadingType
                    , sRepo) =
            {
                ContainerSystem = containerSystem
                AbsoluteFilePath = PathManager.getFullPath (relativeFilePath.ToFile()) (directoryName|>DsDirectory)
                RelativeFilePath = PathManager.changeExtension (relativeFilePath.ToFile()) ".ds"  //pptx로 부터 .ds로 생성
                LoadedName = loadedName
                ShareableSystemRepository =  sRepo

                HostIp = hostIp
                LoadingType = loadingType
            }

    let mutable activeSysDir = ""
    let addLoadedLibSystemNCall(loadedName, apiName, mySys:DsSystem, parentF:Flow option, parentR:Real option, node:pptNode)=
        let paras = getParams(activeSysDir, "./DS_Library.ds"
                    , loadedName, mySys, None, DuDevice, ShareableSystemRepository())
        
        let parent =
            if(parentR.IsSome)
            then  DuParentReal (parentR.Value)
            else  DuParentFlow (parentF.Value)

      
        let devOrg = libDevOrgSys[loadedName]

        if not(mySys.LoadedSystems.Select(fun f->f.Name).Contains(loadedName))
        then 
            mySys.AddLoadedSystem(Device(devOrg, paras))

        let api = devOrg.ApiItems.First(fun f-> f.Name = apiName)
        let devTask = TaskDev(api, "", "", loadedName) :> DsTask
        let job = Job(loadedName+"_"+apiName, [devTask])
        mySys.Jobs.Add(job)
        CallDev.Create(job, parent)
