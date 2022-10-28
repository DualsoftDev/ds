// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open PPTX
open System.Collections.Generic
open Microsoft.FSharp.Collections
open Engine.Common.FS
open Model.Import.Office
open Engine.Core

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint(path:string) =
        let doc   = pptDoc(path)
        let mmodel = MModel(doc.FullPath)
        let coreModel = CoreModule.Model()

        member internal x.GetImportModel() = 
            try
        //new 
                let dicSys = Dictionary<int, DsSystem>()  //0 페이지 기본 나의 시스템 (각페이지별 해당시스템으로 구성)
                let mySystem = DsSystem.Create(TextMySys, "localhost", coreModel)  
                mySystem.Active <- true;
                dicSys.Add(0, mySystem)

                //page 타이틀 이름 중복체크 (없으면 P0, P1, ... 자동생성)
                ImportCheck.CheckMakeSystem(doc) 
                MakeSystem(doc, coreModel, dicSys) //new 
                MakeInterfaces(doc, coreModel, dicSys) //new


                ImportCheck.CheckMakeCopySystemAddApi(doc.Nodes, dicSys) 
                MakeCopySystemAddApi(doc, coreModel, dicSys) //new

                //Flow 리스트 만들기
                let dicFlow = Dictionary<int, Flow>() // page , flow
                MakeFlows(doc.Pages, coreModel, dicFlow) |> ignore //new 

                // system, flow 이름 중복체크 
                ImportCheck.SameSysFlowName(coreModel.Systems, dicFlow) |> ignore //new
                //alias Setting
                MakeAlias    (doc,  dicFlow) //new
                //EMG & Start & Auto 리스트 만들기
                MakeButtons  (doc.Nodes, coreModel, dicFlow) //new
                //let btn   = node.IsEmgBtn || node.IsStartBtn || node.IsAutoBtn || node.IsResetBtn 
                //let bound = if(btn) then ExBtn
                //            else if(node.NodeType.IsCall) then OtherFlow else ThisFlow
            
                //segment 리스트 만들기
                let dicVertex = Dictionary<string, Vertex>()
                MakeSegment(doc.Nodes, coreModel, doc.Parents, dicFlow, dicVertex) //new
                //Safety 만들기
                MakeSafeties(doc.Nodes, coreModel, dicFlow, dicVertex)  //new
                //MakeVetexEdges(doc, coreModel, dicFlow , dicVertex) //new
                MakeEdges     (doc, coreModel, dicFlow ,dicSys,  dicVertex) //new
                MakeApiTxRx(doc, coreModel, dicFlow) //new
         

                  
        //old
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
                coreModel, mmodel

            with ex ->  failwithf  $"{ex.Message}"
                        coreModel, mmodel
                    

    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        DoWork(20);
        let model = ppt.GetImportModel()
        DoWork(50);
        model
        


