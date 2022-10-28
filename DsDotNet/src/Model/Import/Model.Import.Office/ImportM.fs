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
        let mmodel = MModel(doc.FullPath)
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
                doc.MakeCopyApi(model) //new

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

                  
        //old   //Model.Import.Viewer 때문에 임시 살려둠
                let dicSeg = Dictionary<string, MSeg>()
                MSys.Create(TextMySys, true, mmodel) |> ignore  
                MakeExSys(doc, mmodel ) |> ignore//old 삭제예정
                MakeFlos(doc.Pages, mmodel) |> ignore //old 삭제예정
                ImportCheck.SameSysFlow(mmodel.Flows) |> ignore//old 삭제예정
                MakeBtn      (doc.Nodes, mmodel)//old 삭제예정
                MakeSeg(doc.Nodes, mmodel, dicSeg, doc.Parents)//old
                //parent 리스트 만들기
                MakeParent(doc.Nodes, mmodel, dicSeg, doc.Parents)
                MakeSafety(doc.Nodes, mmodel, dicSeg) //old
                  //Dummy child 처리
                MakeDummy(doc.Parents, dicSeg) //old
                //edge 리스트 만들기 
                MakeEdge(doc,  mmodel, dicSeg) //old
                //Root Flow AddSingleNode
                MakeSingleNode(doc,  mmodel, dicSeg)
                //Call 위치정보 업데이트 (마지막 페이지만 정보 반영)
                MakeLayouts(doc, mmodel, dicSeg)
                //중복엣지 체크
                ImportCheck.SameEdgeErr(doc.Edges ) |> ignore
                //유효 이름 체크
                //ImportCheck.ValidPath(doc.Nodes, mmodel) |> ignore
               // ImportCheck.CopySystemErr(doc.Nodes, mmodel) |> ignore
                //exSysCopy  만들기
                MakeCopySys(doc,  mmodel)
                //tx rx 유효 체크 필요 todo
                ImportCheck.InterfaceErr(doc.Nodes, mmodel, dicSeg) |> ignore
                //Interface 리스트 만들기
                MakeIf(doc.Nodes, mmodel, dicSeg) ///old
       
                
             
                MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                model, mmodel

            with ex ->  failwithf  $"{ex.Message}"
                        model, mmodel
                    

    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        DoWork(20);
        let model = ppt.GetImportModel()
        DoWork(50);
        model
        


