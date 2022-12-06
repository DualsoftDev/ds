// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTObjectModule
open System.Collections.Generic
open Engine.Common.FS
open Model.Import.Office
open Engine.Core
open System.IO

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint() =
      //  let configFile = @"test-model-config.json"
      //  let model = ModelLoader.LoadFromConfig configFile

        let rec loadSystem(path:string, name:string, active:bool) = 
            let dicFlow = Dictionary<int, Flow>() // page , flow
            let dicVertex = Dictionary<string, Vertex>()
    
            let doc = pptDoc(path)
            let name, ip = if active //active는 시스템이름으로 ppt 파일 이름을 사용
                            then doc.Name,"localhost" 
                            else name, ""
            let mySys = DsSystem(name, ip)

            doc.GetCopyPathNName()
            |> Seq.iter(fun (path, loadedName) -> 

                    let paras = {   ContainerSystem = mySys
                                    AbsoluteFilePath = Path.GetFullPath(Path.Combine(doc.DirectoryName, path)) + ".pptx"
                                    UserSpecifiedFilePath = path + ".ds"
                                    LoadedName = loadedName }

                    let sys, (doc: pptDoc) = loadSystem(paras.AbsoluteFilePath, loadedName, false)

                    if loadedName =  doc.Name //파일이름 그대로 부르면 ExSys, 다른이름이면 Device
                    then mySys.AddLoadedSystem(ExternalSystem(sys, paras))
                    else mySys.AddLoadedSystem(Device(sys, paras))
                    )


            //page 타이틀 이름 중복체크 (없으면 P0, P1, ... 자동생성)
            //ImportCheck.CheckMakeSystem(doc)
            doc.MakeInterfaces(mySys)

            //ImportCheck.CheckMakeCopyApi(doc.Nodes)
            //Flow 리스트 만들기
            doc.MakeFlows(mySys) |> ignore

            // system, flow 이름 중복체크
            //ImportCheck.SameSysFlowName(mySys.ReferenceSystems, ImportU.dicFlow) |> ignore
            //EMG & Start & Auto 리스트 만들기
            doc.MakeButtons  (mySys)

            //segment 리스트 만들기
            doc.MakeSegment(mySys)

            ImportCheck.SameEdgeErr(doc.Edges) |> ignore

            //Edge  만들기
            doc.MakeEdges (mySys)
            //Safety 만들기
            doc.MakeSafeties(mySys)
            //ApiTxRx  만들기
            doc.MakeApiTxRx()
            
            mySys, doc

        member internal x.GetImportModel(path:string) =
            try

           
                let mySys, doc = loadSystem(path, "", true)
                //Dummy 및 UI Flow, Node, Edge 만들기
                let viewNodes = doc.MakeGraphView(mySys)


                MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                mySys, viewNodes

            with ex ->  failwithf  $"{ex.Message}"


    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint()
        let mySys, viewNodes = ppt.GetImportModel(path)

        mySys, viewNodes



