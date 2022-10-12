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
open Engine.Core

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint(path:string) =
        let doc   = pptDoc(path)
        let model = MModel(doc.FullPath)

        member internal x.GetImportModel() = 
            try
                let dicSeg = ConcurrentDictionary<string, MSeg>()
                MSys.Create(TextMySys, true, model) |> ignore

                //page 타이틀 이름 중복체크 (없으면 P0, P1, ... 자동생성)
                ImportCheck.SamePageErr(doc.Pages) |> ignore

                //ExSys 및 Flow 만들기
                MakeExSys(doc.Pages, model) |> ignore
                //MFlow 리스트 만들기
                MakeFlows(doc.Pages, model) |> ignore
                // system, flow 이름 중복체크 
                ImportCheck.SameSysFlowName(model.Flows) |> ignore
                 

                //alias Setting, Safety & EMG & Start & Auto 리스트 만들기
                MakeAlias    (doc.Nodes, model, doc.Parents)
                MakeBtn      (doc.Nodes, model)

                
                //segment 리스트 만들기
                MakeSegment(doc.Nodes, model, dicSeg, doc.Parents)
                //parent 리스트 만들기
                MakeParent(doc.Nodes, model, dicSeg, doc.Parents)
                
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
                //exSys  만들기
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
        


