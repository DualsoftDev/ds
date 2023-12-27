// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.IO
open System.Text
open System.Data
open System.Runtime.CompilerServices


[<AutoOpen>]
module ExportLayoutTable =
    
    [<Flags>]
    type LayoutColumn =
        | DeviceName = 0
        | FlowName = 1
        | X = 2
        | Y = 3
        | W = 4
        | H = 5


    let ToLayoutTable  (flows:Flow seq) : DataTable =

        let dt = new System.Data.DataTable($"LayoutTable")
        dt.Columns.Add($"{LayoutColumn.DeviceName}", typeof<string>) |> ignore
        dt.Columns.Add($"{LayoutColumn.FlowName}", typeof<string>) |> ignore
        dt.Columns.Add($"{LayoutColumn.X}", typeof<string>) |> ignore
        dt.Columns.Add($"{LayoutColumn.Y}", typeof<string>) |> ignore
        dt.Columns.Add($"{LayoutColumn.W}", typeof<string>) |> ignore
        dt.Columns.Add($"{LayoutColumn.H}", typeof<string>) |> ignore

        let rowItem (deviceName:string, flow:Flow, xywh:Xywh) =
            [ 
              deviceName
              $"{flow.Name}"
              $"{xywh.X}"
              $"{xywh.Y}"
              $"{xywh.W}"
              $"{xywh.H}"
              ]

        let rows = flows.SelectMany(fun f -> 
                        f.GetVerticesOfFlow().OfType<Call>().SelectMany(fun c->
                                c.TargetJob.DeviceDefs.Select(fun d-> rowItem (d.DeviceName,f,d.ApiItem.Xywh))
                        ))
        rows
        |> Seq.distinctBy (fun row -> row.First())      //디바이스 이름기준으로 중복 제거
        |> Seq.iter (fun row ->
            let rowTemp = dt.NewRow()
            rowTemp.ItemArray <- (row |> Seq.cast<obj> |> Seq.toArray)
            dt.Rows.Add(rowTemp) |> ignore)
        dt

    
