// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTObjectModule
open System.Collections.Generic
open Engine.Common.FS
open Model.Import.Office
open Engine.Core
open System.IO
open Engine.Core.ModelLoaderModule
open System.Runtime.CompilerServices

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint() =
        let pathStack = Stack<string>()

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
            if   nodeType = COPY_REF   then DuExternal 
            elif nodeType = COPY_VALUE then DuDevice
            else  failwith "error"


        let rec loadSystem(repo:ShareableSystemRepository, pptReop:Dictionary<DsSystem, pptDoc>, theSys:DsSystem, paras:DeviceLoadParameters) =
            pathStack.Push(paras.AbsoluteFilePath)

            let doc = pptDoc(paras.AbsoluteFilePath+".pptx" , paras)
            //시스템 로딩시 중복이름을 부를 수 없다.
            CheckSameCopy(doc)

            let reloading(newSys:DsSystem, paras) =
                let (sys, newDoc:pptDoc) = loadSystem(repo, pptReop, newSys, paras)
                sys
        
            doc.GetCopyPathNName()
            |> Seq.iter(fun (userPath, loadedName, node) ->
                
                let paras = getParams(repo, doc.DirectoryName, userPath
                            , loadedName, theSys, None, getLoadingType node.NodeType)
                let hostIp = if paras.HostIp.IsSome then paras.HostIp.Value else ""

                match node.NodeType with
                |COPY_REF ->
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

                |COPY_VALUE ->
                    let newDevSys = DsSystem(paras.LoadedName, hostIp)
                    let sys = reloading(newDevSys, paras)
                    theSys.AddLoadedSystem(Device(sys, paras))

                |_ ->   failwith "error"
            )

            //ExternalSystem 위하여 인터페이스는 공통으로 시스템 생성시 만들어 줌
            doc.MakeInterfaces(theSys)
            doc.MakeInterfaceResets(theSys)

            if paras.LoadingType <> DuExternal   
            then //External system 은 Interfaces만 만들고 나중에 buildSystem 수행
                doc.BuildSystem(theSys)

            pathStack.Pop() |> ignore
            pptReop.Add (theSys, doc)
            theSys, doc

        member internal x.GetImportModel(systemRepo:ShareableSystemRepository, pptReop:Dictionary<DsSystem, pptDoc>,  path:string, loadingType:ParserLoadingType) =
            try
                //active는 시스템이름으로 ppt 파일 이름을 사용
                let mySys = DsSystem(getSystemName path, "localhost")
                let paras = getParams(systemRepo
                            , getSystemDirectoryName path
                            , getSystemName path
                            , mySys.Name
                            , mySys
                            , Some mySys.HostIp
                            , DuNone)

                let mySys, doc = loadSystem(systemRepo, pptReop, mySys, paras)
                //Dummy 및 UI Flow, Node, Edge 만들기
                let viewNodes = doc.MakeGraphView(mySys)


                //MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                //MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                //MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                //MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                mySys, viewNodes

            with ex ->  failwithf  $"{ex.Message}\t [ErrPath:{pathStack.First()}]"
            
    let private fromPPTs(paths:string seq) =
        let systemRepo = ShareableSystemRepository()
        let pptRepo    = Dictionary<DsSystem, pptDoc>()

        let cfg = {DsFilePaths = paths |> Seq.toList}

        let results =
            [  
                for dsFile in cfg.DsFilePaths do
                      let ppt = ImportPowerPoint()
                      ppt.GetImportModel(systemRepo, pptRepo, dsFile, ParserLoadingType.DuNone)
            ]

        //ExternalSystem 순환참조때문에 완성못한 시스템 BuildSystem 마무리하기
        pptRepo
            .Where(fun dic -> not <| dic.Value.IsBuilded)
            .ForEach(fun dic -> 
                let dsSystem = dic.Key
                let pptDoc = dic.Value
                pptDoc.BuildSystem(dsSystem))

        let systems =  results.Select(fun (sys, view) -> sys) |> Seq.toList
        let views   =  results |> dict
        let storages = systems |> Seq.map(fun s -> s, new Storages()) |> dict
        { Config = cfg; Systems = systems; Storages = storages}, views, pptRepo
    
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
                    -> pptRepo 
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
                               
                            system.ToDsText(), rootDirectroy, relative)
