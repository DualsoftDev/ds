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
        let pathStack = Stack<string>()

        let getParams(directoryName:string, filePath:string, loadedName:string, containerSystem:DsSystem) = 
            {  
                ContainerSystem = containerSystem
                AbsoluteFilePath = Path.GetFullPath(Path.Combine(directoryName, filePath)) + ".pptx"
                UserSpecifiedFilePath = filePath + ".ds"
                LoadedName = loadedName 
            }

        let rec loadSystem(path:string, name:string, active:bool) = 
            pathStack.Push(path)

            let doc = pptDoc(path)
            let name, ip =  if active //active는 시스템이름으로 ppt 파일 이름을 사용
                            then doc.Name, "localhost" 
                            else name, ""
            let mySys = DsSystem(name, ip)

            doc.GetCopyPathNName()
            |> Seq.iter(fun (path, loadedName) -> 

                    let paras = getParams(doc.DirectoryName, path, loadedName, mySys)
                    let sys, (doc: pptDoc) = loadSystem(paras.AbsoluteFilePath, loadedName, false)

                    if loadedName =  doc.Name //파일이름 그대로 부르면 ExSys, 다른이름이면 Device
                    then mySys.AddLoadedSystem(ExternalSystem(sys, paras))
                    else mySys.AddLoadedSystem(Device(sys, paras))
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

            with ex ->  failwithf  $"{pathStack.Last()}\r\n{ex.Message}"


    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint()
        let mySys, viewNodes = ppt.GetImportModel(path)

        mySys, viewNodes



