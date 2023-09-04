// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTObjectModule
open System.Collections.Generic
open Dual.Common.Core.FS
open Model.Import.Office
open Engine.Core
open System.IO
open Engine.Core.ModelLoaderModule
open System.Runtime.CompilerServices
open DocumentFormat.OpenXml.Packaging

[<AutoOpen>]
module ImportPPTModule =

    let dicPptDoc = Dictionary<string, PresentationDocument>()
    let pathStack = Stack<string>()
    type internal ImportPowerPoint() =

        let getParams(systemRepo:ShareableSystemRepository, directoryName:string
                    , userPath:string, loadedName:string, containerSystem:DsSystem
                    , hostIp:string option, loadingType) =
            {
                ContainerSystem = containerSystem
                AbsoluteFilePath = Path.GetFullPath(Path.Combine(directoryName, userPath))
                UserSpecifiedFilePath = userPath + ".ds"
                LoadedName = loadedName
                ShareableSystemRepository = systemRepo

                HostIp = hostIp
                LoadingType = loadingType
            }

        let getLoadingType(nodeType:NodeType) =
            match nodeType with
            | OPEN_SYS
            | OPEN_CPU -> DuExternal
            | COPY_SYS -> DuExternal
            | _ -> failwithlog "error"


        let rec loadSystem(repo:ShareableSystemRepository, pptReop:Dictionary<DsSystem, pptDoc>, theSys:DsSystem, paras:DeviceLoadParameters) =
            pathStack.Push(paras.AbsoluteFilePath)

            let pathPPT = paras.AbsoluteFilePath+".pptx"

            let doc =
                if dicPptDoc.ContainsKey pathPPT
                then pptDoc(pathPPT , paras, dicPptDoc[pathPPT])
                else
                    let doc = Office.Open(pathPPT)
                    let pptDoc = pptDoc(pathPPT , paras, doc)
                    dicPptDoc.Add(pathPPT, doc) 
                    pptDoc

            //시스템 로딩시 중복이름을 부를 수 없다.
            CheckSameCopy(doc)
            SameFlowName(doc)
            
            let reloading(newSys:DsSystem, paras) =
                let (sys, newDoc:pptDoc) = loadSystem(repo, pptReop, newSys, paras)
                sys

            doc.GetCopyPathNName()
            |> Seq.iter(fun (userPath, loadedName, node) ->

                let path =  Path.GetFullPath(Path.Combine(doc.DirectoryName, userPath))
                
                let paras = getParams(repo, doc.DirectoryName, userPath
                            , loadedName, theSys, None, getLoadingType node.NodeType)
                let hostIp = if paras.HostIp.IsSome then paras.HostIp.Value else ""

                match node.NodeType with
                |OPEN_CPU
                |OPEN_SYS ->
                    let exLoaded =
                        if repo.ContainsKey paras.AbsoluteFilePath
                        then
                            ExternalSystem(repo[paras.AbsoluteFilePath], paras)
                        else
                            let newExSys = DsSystem(paras.LoadedName, hostIp)
                            repo.Add (paras.AbsoluteFilePath, newExSys)

                            let sys = reloading(newExSys, paras)
                            let exNewLoad = ExternalSystem(sys, paras)
                            exNewLoad

                    theSys.AddLoadedSystem(exLoaded)

                |COPY_SYS ->
                    let newDevSys = DsSystem(paras.LoadedName, hostIp)
                    let sys = reloading(newDevSys, paras)
                    theSys.AddLoadedSystem(Device(sys, paras))

                |_ ->   failwithlog "error"
            )

            //ExternalSystem 위하여 인터페이스는 공통으로 시스템 생성시 만들어 줌
            doc.MakeInterfaces(theSys)
            doc.MakeInterfaceResets(theSys)

            if paras.LoadingType = DuNone || paras.LoadingType  = DuDevice 
            then //External system 은 Interfaces만 만들고 나중에 buildSystem 수행
                doc.BuildSystem(theSys)

            if paras.LoadingType = DuNone 
            then //active system 은 Update IO
                doc.UpdateActionIO(theSys)


            pathStack.Pop() |> ignore
            pptReop.Add (theSys, doc)
            theSys, doc

        member internal x.GetImportModel(systemRepo:ShareableSystemRepository, pptReop:Dictionary<DsSystem, pptDoc>,  path:string) =
                //active는 시스템이름으로 ppt 파일 이름을 사용
                let mySys = DsSystem(getSystemName path, "localhost")
                let paras = getParams(systemRepo
                            , getSystemDirectoryName path
                            , getSystemName path
                            , mySys.Name
                            , mySys
                            , Some mySys.HostIp
                            , DuNone)

                loadSystem(systemRepo, pptReop, mySys, paras)
              
         
    let private fromPPTs(paths:string seq) =
        try
            dicPptDoc.Clear()
            let systemRepo = ShareableSystemRepository()
            let pptRepo    = Dictionary<DsSystem, pptDoc>()

            let cfg = {DsFilePaths = paths |> Seq.toList}

            let results =
                [
                    for dsFile in cfg.DsFilePaths do
                          let ppt = ImportPowerPoint()
                          ppt.GetImportModel(systemRepo, pptRepo, dsFile)
                ]

            //ExternalSystem 순환참조때문에 완성못한 시스템 BuildSystem 마무리하기
            pptRepo
                .Where(fun dic -> not <| dic.Value.IsBuilded)
                .ForEach(fun dic ->
                    let dsSystem = dic.Key
                    let pptDoc = dic.Value
                    pathStack.Push(pptDoc.Path)
                    pptDoc.BuildSystem(dsSystem)
                    pathStack.Pop() |> ignore
                    )

            //사용한 ppt doc 일괄 닫기 (열린문서 재 사용이 있어서 사용후 전체 한번에 닫기)
            dicPptDoc.Iter(fun f->f.Value.Close())

            let systems =  results.Select(fun (sys, view) -> sys) |> Seq.toList
            let views   =  results |> dict
            { Config = cfg; Systems = systems}, views, pptRepo

        with ex ->  
            dicPptDoc.Iter(fun f->f.Value.Close())
            failwithf  @$"{ex.Message} [ErrPath:{if pathStack.any() then pathStack.First() else paths.First() }]" //첫페이지 아니면 stack에 존재

    type PptResult = {
        System: DsSystem
        Views : ViewNode seq
        IsActive : bool
    }

    [<Extension>]
    type ImportPPT =

        [<Extension>]
        static member GetModel      (paths:string seq) =
            fromPPTs paths |> fun (model, views, pptRepo) -> model

        [<Extension>]
        static member GetModelNView (paths:string seq) =
            fromPPTs paths |> fun (model, views, pptRepo) -> model, views

        [<Extension>]
        static member GetLoadingAllSystem (paths:string seq) =
             fromPPTs paths
             |> fun (model, views, pptRepo)
                    -> model, pptRepo
                        |> Seq.map(fun f->
                            {   System= f.Key
                                Views = f.Value.MakeGraphView(f.Key)
                                IsActive = model.Systems.Contains(f.Key)}  )
        [<Extension>]
        static member GetDsFilesWithLib (paths:string seq) =
                fromPPTs paths
                |> fun (model, views, pptRepo)
                    -> pptRepo
                         .Select(fun dic ->
                            let system = dic.Key
                            let param = dic.Value.Parameter
                            let relative  = param.UserSpecifiedFilePath
                            let absolute  = param.AbsoluteFilePath
                            let removeTarget = relative.Replace("ds", "")
                            let rootDirectroy = absolute.Substring(0, absolute.length() - removeTarget.length())

                            system.ToDsText(true), rootDirectroy, relative)
