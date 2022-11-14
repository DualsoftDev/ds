// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTObjectModule
open System.Collections.Generic
open Engine.Common.FS
open Model.Import.Office
open Engine.Core

[<AutoOpen>]
module ImportM =

    type internal ImportPowerPoint(path:string) =
        let doc   = pptDoc(path)
        let mutable theSystem:DsSystem option = None

        member internal x.GetImportModel() =
            try

                ImportU.dicSys.Clear()
                ImportU.dicCopy.Clear()
                ImportU.dicFlow.Clear()
                ImportU.dicVertex.Clear()
                theSystem <- DsSystem.Create(doc.Name,"localhost")

                model.TheSystem  <- Some topSystem
                topSystem.Active <- true
                ImportU.dicSys.Add(0, topSystem)

                //page 타이틀 이름 중복체크 (없으면 P0, P1, ... 자동생성)
                ImportCheck.CheckMakeSystem(doc)
                doc.MakeSystem(model)
                doc.MakeCopySystem(model)
                doc.MakeInterfaces()

                ImportCheck.CheckMakeCopyApi(doc.Nodes, ImportU.dicSys)
                //Flow 리스트 만들기
                doc.MakeFlows(model) |> ignore

                // system, flow 이름 중복체크
                ImportCheck.SameSysFlowName(model.Systems, ImportU.dicFlow) |> ignore
                //EMG & Start & Auto 리스트 만들기
                doc.MakeButtons  (model)

                //segment 리스트 만들기
                doc.MakeSegment(model)

                ImportCheck.SameEdgeErr(doc.Edges) |> ignore

                //Edge  만들기
                doc.MakeEdges (model)
                //Safety 만들기
                doc.MakeSafeties(model)
                //ApiTxRx  만들기
                doc.MakeApiTxRx(model)
                //Dummy 및 UI Flow, Node, Edge 만들기
                let viewNodes = doc.MakeGraphView(model)


                MSGInfo($"전체 장표   count [{doc.Pages.Count()}]")
                MSGInfo($"전체 도형   count [{doc.Nodes.Count()}]")
                MSGInfo($"전체 연결   count [{doc.Edges.Count()}]")
                MSGInfo($"전체 부모   count [{doc.Parents.Keys.Count}]")
                model, viewNodes

            with ex ->  failwithf  $"{ex.Message}"
                        model, null


    let FromPPTX(path:string) =
        let ppt = ImportPowerPoint(path)
        DoWork(20);
        let model = ppt.GetImportModel()
        DoWork(50);
        model



