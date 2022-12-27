// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open Microsoft.Office.Interop.Excel
open System.Drawing
open System.Reflection
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module ExportIOTable =

    let ToTable(sys:DsSystem) =

        let dt = new System.Data.DataTable($"{sys.Name}")
        dt.Columns.Add($"{IOColumn.Case}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Name}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Type}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Size}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Output}"     , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Input}"      , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Command}"    , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Observe}"    , typeof<string>) |>ignore

        let rowItems(jobDef:JobDef) =
            ["주소";  jobDef.ApiName; "IO"; "bit"; jobDef.OutTag  ; jobDef.InTag; jobDef.CommandOutTimming; jobDef.ObserveInTimming]

        let rows =
            seq {
                for job in sys.Jobs do
                    for jobDef in job.JobDefs do
                        yield rowItems(jobDef)
            }

        rows
        |> Seq.iter(fun row -> 
                    let rowTemp = dt.NewRow()
                    rowTemp.ItemArray <- (row|> Seq.cast<obj>|> Seq.toArray)
                    dt.Rows.Add(rowTemp) |> ignore)
       
        for btn in  sys.EmergencyButtons do
            dt.Rows.Add(TextEmgBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.AutoButtons do
            dt.Rows.Add(TextAutoBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.RunButtons do
            dt.Rows.Add(TextStartBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.ClearButtons do
            dt.Rows.Add(TextResetBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.ManualButtons do
            dt.Rows.Add(TextResetBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.StopButtons    do
            dt.Rows.Add(TextResetBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.DryRunButtons do
            dt.Rows.Add(TextResetBtn,  btn.Name  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore

        dt.Rows.Add("'-", "'-","'-", "'-", "'-","'-", "'-","'-") |> ignore
        dt.Rows.Add("'-", "'-","'-", "'-", "'-","'-", "'-","'-") |> ignore
        dt.Rows.Add("'-", "'-","'-", "'-", "'-","'-", "'-","'-") |> ignore

        dt.Rows.Add(TextVariable,  ""  , "'-", ""  , "'-", "'-", "'-", "'-") |> ignore
        dt

    let ToFiie(systems:DsSystem seq, excelFilePath:string) = 
        let disableCellStyle(range:Microsoft.Office.Interop.Excel.Range ) =
            range.Interior.Color <- Color.LightGray;
            range.Font.Bold <- true;
            range.Borders.LineStyle <- XlLineStyle.xlContinuous;
            range.Borders.Weight <- 2;

        let cellMerge(range:Microsoft.Office.Interop.Excel.Range ) =
            range.Merge(true)

        let enableCellStyle(range:Microsoft.Office.Interop.Excel.Range ) =
                   range.Interior.Color <- Color.LightYellow;
                   range.Borders.LineStyle <- XlLineStyle.xlDash;

        let autoFitNFilterColumn(range:Microsoft.Office.Interop.Excel.Range ) =
                   range.AutoFilter(1, Type.Missing, XlAutoFilterOperator.xlAnd, Type.Missing, true) |> ignore
                   range.EntireColumn.AutoFit() |> ignore
        
        let mutable curRow = 0
        let createWorkSheet(tbl:Data.DataTable, wb:Workbook, totalRows:int) = 
            let rowsCnt = tbl.Rows.Count
            let colsCnt = tbl.Columns.Count
            //  create worksheet
            let workSheet = wb.Worksheets.Add Missing.Value :?> Worksheet
            workSheet.Name <- tbl.TableName
            
            //// now we resize the columns  
            let excelCellrange = workSheet.Range(workSheet.Cells.[1, 1], workSheet.Cells.[rowsCnt+ 1, colsCnt]);
            disableCellStyle(excelCellrange) 
        
            for colIndex in [|0..colsCnt-1|] do
                workSheet.Cells.[1,colIndex+1] <- tbl.Columns.[colIndex].ColumnName
            //// rows
            for rowsIndex in [|0..rowsCnt-1|] do
                curRow<-curRow+1
                DoWork(Convert.ToSingle(curRow) / Convert.ToSingle(totalRows) * 100f |> int);
                for colIndex in [|0..colsCnt-1|] do
                    workSheet.Cells.[rowsIndex + 2, colIndex + 1] <- tbl.Rows.[rowsIndex].[colIndex]
                    let cellText =tbl.Rows.[rowsIndex].[colIndex].ToString()
                    if(cellText = "" || (cellText.StartsWith("'") && cellText.StartsWith("'-")|>not))
                    then 
                        let excelCellrange = workSheet.Range(workSheet.Cells.[rowsIndex + 2, colIndex + 1], workSheet.Cells.[rowsIndex + 2, colIndex + 1])
                        enableCellStyle(excelCellrange)

                    if(colIndex = 0 && (cellText = TextCommand||cellText = TextObserve))
                    then cellMerge(workSheet.Range(workSheet.Cells.[rowsIndex + 2, colIndex + 6], workSheet.Cells.[rowsIndex + 2, colIndex + 9]))

            workSheet.Range(workSheet.Cells.[1, 1], workSheet.Cells.[rowsCnt + 1, colsCnt]) |> autoFitNFilterColumn


        // load excel, and create a new workbook
        let excelApp = new ApplicationClass(Visible = false)
        let wb = excelApp.Workbooks.Add Missing.Value
        let firstSheet = wb.ActiveSheet :?> Worksheet 
        excelApp.ScreenUpdating <- false 

        let tables =  systems 
                        |> Seq.map(fun s ->  ToTable s) 
                        |> Seq.sortBy(fun f->f.TableName)

        let totalRows = tables 
                        |> Seq.map(fun t -> t.Rows.Count)
                        |> Seq.sum

        tables 
        |> Seq.iter(fun d -> createWorkSheet (d, wb, totalRows))

            //처음 자동으로 생성된 빈 Sheet 삭제
        firstSheet.Delete()
        wb.SaveAs(excelFilePath)
        excelApp.Quit()


    


