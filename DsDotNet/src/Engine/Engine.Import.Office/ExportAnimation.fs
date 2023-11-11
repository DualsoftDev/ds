// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open System.Drawing
open System.Reflection
open Dual.Common.Core.FS
open Engine.Core
open System.IO
open System.Text
open System.Data


[<AutoOpen>]
module ExportAnimation =

    let ToJson(system:DsSystem, flowName:string) =
        let dataTable = ToTable system
        let csvContent = new StringBuilder()
            // 컬럼 헤더 추가
        let columnNames = dataTable.Columns |> Seq.cast<DataColumn> |> Seq.map (fun col -> col.ColumnName)
        csvContent.AppendLine(String.Join("\t", columnNames |> Seq.toArray)) |> ignore

            // 각 행의 데이터 추가
        dataTable.Rows |> Seq.cast<DataRow> |> Seq.iter (fun row ->
            let fieldValues = row.ItemArray |> Seq.map (fun obj ->
                let field = obj.ToString()
        
                field.Replace("\t", "\\t") // 새 줄 문자 처리
            )
            csvContent.AppendLine(String.Join("\t", fieldValues |> Seq.toArray)) |> ignore
        )

        // 임시 파일 경로를 생성
        let tempFilePath = Path.Combine(Path.GetTempPath(), "DSAnimation.json")
        // CSV 내용을 파일에 씀
        File.WriteAllText(tempFilePath, csvContent.ToString())
        tempFilePath

