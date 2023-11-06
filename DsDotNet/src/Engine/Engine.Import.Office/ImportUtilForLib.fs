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



    type DevInfo = {
        DevName: string
        ApiName: string
    }
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

    let addLoadedLibSystemNCall(loadedName, apiName, mySys:DsSystem, parentF:Flow option, parentR:Real option, node:pptNode)=
        let libFilePath =  PathManager.getFullPath  ($"./{TextLibrary}.ds"|>DsFile ) (activeSysDir|>DsDirectory)
        let libRelPath    =   PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile);
        let paras = getParams(activeSysDir, libRelPath
                    , loadedName, mySys, None, DuDevice, ShareableSystemRepository())
        
        let parent =
            if(parentR.IsSome)
            then  DuParentReal (parentR.Value)
            else  DuParentFlow (parentF.Value)

        let systems, loadingPaths = ParserLoader.LoadFromActivePath libFilePath
        let devOrg =  systems |> Seq.head
        if not(mySys.LoadedSystems.Select(fun f->f.Name).Contains(loadedName))
        then 
            mySys.AddLoadedSystem(Device(devOrg, paras))

        let api = devOrg.ApiItems.First(fun f-> f.Name = apiName)
        let devTask = TaskDev(api, "", "", loadedName) :> DsTask
        let job = Job(loadedName+"_"+apiName, [devTask])
        mySys.Jobs.Add(job)
        CallDev.Create(job, parent)
