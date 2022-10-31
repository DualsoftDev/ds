// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTX
open System.Collections.Generic
open Engine.Common.FS
open Model.Import.Office
open Engine.Core

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint(path:string) =
        let doc   = pptDoc(path)
        let model = CoreModule.Model()

        member internal x.GetImportModel() = 
            try
        //new 
                ImportU.dicSys.Clear()
                ImportU.dicCopy.Clear()
                ImportU.dicFlow.Clear()
                ImportU.dicVertex.Clear()

                let mySystem = DsSystem.Create(TextMySys, "localhost", model)  
                mySystem.Active <- true         
                ImportU.dicSys.Add(0, mySystem)

                //page 타이틀 이름 중복체크 (없으면 P0, P1, ... 자동생성)
                ImportCheck.CheckMakeSystem(doc) 
                doc.MakeSystem(model) //new 
                doc.MakeCopySystem(model) //new 
                doc.MakeInterfaces() //new

                ImportCheck.CheckMakeCopyApi(doc.Nodes, ImportU.dicSys) 
                //Flow 리스트 만들기
                doc.MakeFlows(model) |> ignore //new 

                // system, flow 이름 중복체크 
                ImportCheck.SameSysFlowName(model.Systems, ImportU.dicFlow) |> ignore //new
                //EMG & Start & Auto 리스트 만들기
                doc.MakeButtons  (model) //new

                //segment 리스트 만들기
                doc.MakeSegment(model) //new
                //Edge  만들기
                doc.MakeEdges (model) //new
                 //Safety 만들기
                doc.MakeSafeties(model)  //new
                //ApiTxRx  만들기
                doc.MakeApiTxRx(model) //new

                  
             
                MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                model, ImportU.dicFlow

            with ex ->  failwithf  $"{ex.Message}"
                        model, ImportU.dicFlow
                    

    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        DoWork(20);
        let model = ppt.GetImportModel()
        DoWork(50);
        model
        


