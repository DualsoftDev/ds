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
[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint() =
      //  let configFile = @"test-model-config.json"
      //  let model = ModelLoader.LoadFromConfig configFile
        let pathStack = Stack<string>()

        let getParams(systemRepo:ShareableSystemRepository, directoryName:string, filePath:string, loadedName:string, containerSystem:DsSystem) =
            {
                ContainerSystem = containerSystem
                AbsoluteFilePath = Path.GetFullPath(Path.Combine(directoryName, filePath)) + ".pptx"
                UserSpecifiedFilePath = filePath + ".ds"
                LoadedName = loadedName
                ShareableSystemRepository = systemRepo

                // <ahn> : External system loading 할 때의 ip 주소 설정 필요
                HostIp = None
                LoadingType = DuNone
                // <ahn>
            }

        let rec loadSystem(systemRepo:ShareableSystemRepository, path:string, name:string, active:bool) =
            pathStack.Push(path)

            let doc = pptDoc(path)
            //시스템 로딩시 중복이름을 부를 수 없다.
            CheckSameCopy(doc)
            let name, ip =  if active //active는 시스템이름으로 ppt 파일 이름을 사용
                            then doc.Name, "localhost"
                            else name, ""

            let mySys = DsSystem(name, ip)
            let reloadingSystem(paras, loadedName) =
                let sys, doc = loadSystem(systemRepo, paras.AbsoluteFilePath, loadedName, false)
                sys

            doc.GetCopyPathNName()
            |> Seq.iter(fun (path, loadedName, node) ->

                let paras = getParams(systemRepo, doc.DirectoryName, path, loadedName, mySys)

                if pathStack.Contains paras.AbsoluteFilePath
                then Office.ErrorPath(node.Shape, ErrID._45, node.PageNum, paras.AbsoluteFilePath)

                let sys = reloadingSystem(paras, loadedName)

                if node.NodeType = COPY_REF
                then mySys.AddLoadedSystem(ExternalSystem(sys, paras))

                if node.NodeType = COPY_VALUE
                then mySys.AddLoadedSystem(Device(sys, paras))
                )

            doc.MakeJobs(mySys)
            doc.MakeInterfaces(mySys)
            doc.MakeFlows(mySys) |> ignore

            //EMG & Start & Auto 리스트 만들기
            doc.MakeButtons(mySys)
            //segment 리스트 만들기
            doc.MakeSegment(mySys)
            //Edge  만들기
            doc.MakeEdges (mySys)
            //Safety 만들기
            doc.MakeSafeties(mySys)
            //ApiTxRx  만들기
            doc.MakeApiTxRx()

            pathStack.Pop() |> ignore

            mySys, doc

        member internal x.GetImportModel(systemRepo:ShareableSystemRepository, path:string) =
            try


                let mySys, doc = loadSystem(systemRepo, path, "", true)
                //Dummy 및 UI Flow, Node, Edge 만들기
                let viewNodes = doc.MakeGraphView(mySys)

                //MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                //MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                //MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                //MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                mySys, viewNodes

            with ex ->  failwithf  $"{ex.Message}\t [ErrPath:{pathStack.First()}]"


    let FromPPTX (systemRepo:ShareableSystemRepository) (path:string) =
        let ppt = ImportPowerPoint()
        let mySys, viewNodes = ppt.GetImportModel(systemRepo, path)

        mySys, viewNodes

    let FromPPTXS(paths:string seq) =
        let systemRepo = ShareableSystemRepository()
        let cfg = {DsFilePaths = paths |> Seq.toList}

        let results =
            [   for dsFile in cfg.DsFilePaths do
                    FromPPTX systemRepo dsFile |> fun (sys, view) -> sys, view ]
        let systems =  results.Select(fun (sys, view) -> sys) |> Seq.toList
        let views   =  results |> dict
        { Config = cfg; Systems = systems}, views


