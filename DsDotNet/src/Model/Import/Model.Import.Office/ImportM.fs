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
        let model = MModel(doc.FullPath)
        let coreModel = CoreModule.Model()

        member internal x.GetImportModel() = 
            try
        
                let dicVertex = ConcurrentDictionary<string, Vertex>()
                let dicSeg = ConcurrentDictionary<string, MSeg>()
                MSys.Create(TextMySys, true, model) |> ignore
                let mySystem = DsSystem.Create(TextMySys, "localhost", None, coreModel)  //new 
                mySystem.Active <- true;

                //page 타이틀 이름 중복체크 (없으면 P0, P1, ... 자동생성)
                ImportCheck.SamePageErr(doc.Pages) |> ignore

                //ExSys  만들기
                MakeExSys(doc, model) |> ignore//old 삭제예정
                MakeExSystem(doc, coreModel) |> ignore //new 


                //Flow 리스트 만들기
                MakeFlos(doc.Pages, model) |> ignore //old 삭제예정
                let dicFlow = Dictionary<int, Flow>() // page , flow
                MakeFlows(doc.Pages, coreModel, dicFlow) |> ignore //new 

                // system, flow 이름 중복체크 
                ImportCheck.SameSysFlow(model.Flows) |> ignore//old 삭제예정
                ImportCheck.SameSysFlowName(coreModel.Systems, dicFlow) |> ignore //new
                 

                //alias Setting, Safety & EMG & Start & Auto 리스트 만들기
                MakeAlias    (doc.Nodes, model, doc.Parents) //new

                MakeBtn      (doc.Nodes, model)//old 삭제예정
                MakeButtons  (doc.Nodes, coreModel, dicFlow) //new

                
                //segment 리스트 만들기
                MakeSeg(doc.Nodes, model, dicSeg, doc.Parents)//old
                MakeSegment(doc.Nodes, coreModel, dicVertex, dicFlow) //new



                //parent 리스트 만들기
                MakeParent(doc.Nodes, model, dicSeg, doc.Parents)
                MakeParents(doc.Nodes, coreModel, dicVertex, doc.Parents)
                

                //Safety 만들기
                MakeSafety(doc.Nodes, model, dicSeg)
                //Dummy child 처리
                MakeDummy(doc.Parents, dicSeg)
                //edge 리스트 만들기 
                MakeEdges(doc,  model, dicSeg)
                //Root Flow AddSingleNode
                MakeSingleNode(doc,  model, dicSeg)


                //tx rx 유효 체크 필요 todo
                ImportCheck.InterfaceErr(doc.Nodes, model, dicSeg) |> ignore
                //Interface 리스트 만들기
                MakeInterface(doc.Nodes, model, dicSeg)
                
                ImportCheck.CopySystemErr(doc.Nodes, model) |> ignore
                //exSysCopy  만들기
                MakeCopySystem(doc,  model)
             
                //유효 이름 체크
                ImportCheck.ValidPath(doc.Nodes, model) |> ignore
                //중복엣지 체크
                ImportCheck.SameEdgeErr(doc.Edges ) |> ignore

                //Call 위치정보 업데이트 (마지막 페이지만 정보 반영)
                MakeLayouts(doc, model, dicSeg)
            
                MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                model

            with ex ->  failwithf  $"{ex.Message}"
                        model
                    

    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        DoWork(20);
        let model = ppt.GetImportModel()
        DoWork(50);
        model
        


