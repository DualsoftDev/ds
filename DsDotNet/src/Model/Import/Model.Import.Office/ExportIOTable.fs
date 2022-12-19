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
        //let sampleCommandName = "'CMD1"
        //let sampleCommand     = "'@Delay(0)"
        //let sampleConditionName = "'CON1"
        //let sampleCondition = "'@Delay(0)"

        let dt = new System.Data.DataTable($"{sys.Name}")
        dt.Columns.Add($"{IOColumn.Case}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Flow}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Name}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Type}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Size}"       , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Output}"     , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Input}"      , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Command}"    , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Observe}"    , typeof<string>) |>ignore

        let rowItems(jobDef:JobDef) =
            let MFlowName, name =  "_", jobDef.ApiName
            ["주소"; MFlowName; name; "IO"; "bit"; jobDef.OutTag  ; jobDef.InTag; ""; ""]

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
            dt.Rows.Add(TextButton, TextEmgBtn, btn.Key  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.AutoButtons do
            dt.Rows.Add(TextButton, TextAutoBtn, btn.Key  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.StartButtons do
            dt.Rows.Add(TextButton, TextStartBtn, btn.Key  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore
        for btn in  sys.ResetButtons do
            dt.Rows.Add(TextButton, TextResetBtn, btn.Key  , "'-", "'-", "'-",  "" , "'-", "'-" ) |> ignore

        dt.Rows.Add("'-", "'-", "'-","'-", "'-", "'-","'-", "'-","'-") |> ignore
        dt.Rows.Add("'-", "'-", "'-","'-", "'-", "'-","'-", "'-","'-") |> ignore
        dt.Rows.Add("'-", "'-", "'-","'-", "'-", "'-","'-", "'-","'-") |> ignore

        dt.Rows.Add(TextVariable, "변수", ""  , "'-", ""  , "'-", "'-", "'-", "'-") |> ignore
        //dt.Rows.Add(TextCommand, "함수", sampleCommandName    , "'-", "'-", sampleCommand  , "'-") |> ignore
        //dt.Rows.Add(TextObserve, "함수", sampleConditionName  , "'-", "'-", sampleCondition, "'-") |> ignore

        dt

 


    let ToFiie(sys:DsSystem, excelFilePath:string) = 
        let DisableCellStyle(range:Microsoft.Office.Interop.Excel.Range ) =
            range.Interior.Color <- Color.LightGray;
            range.Font.Bold <- true;
            range.Borders.LineStyle <- XlLineStyle.xlContinuous;
            range.Borders.Weight <- 2;

        let CellMerge(range:Microsoft.Office.Interop.Excel.Range ) =
            range.Merge(true)

        let EnableCellStyle(range:Microsoft.Office.Interop.Excel.Range ) =
                   range.Interior.Color <- Color.LightYellow;
                   range.Borders.LineStyle <- XlLineStyle.xlDash;

        let AutoFitNFilterColumn(range:Microsoft.Office.Interop.Excel.Range ) =
                   range.AutoFilter(1, Type.Missing, XlAutoFilterOperator.xlAnd, Type.Missing, true) |> ignore
                   range.EntireColumn.AutoFit() |> ignore


        let tbl = ToTable(sys)
        if (tbl = null || tbl.Columns.Count = 0)
        then failwithf "ExportToExcel: Null or empty input table!\n"
        else 
            // load excel, and create a new workbook
            let excelApp = new ApplicationClass(Visible = false)
            excelApp.ScreenUpdating = false |> ignore
            let wb = excelApp.Workbooks.Add Missing.Value

            let rowsCnt = tbl.Rows.Count
            let colsCnt = tbl.Columns.Count
            
            // single worksheet
            let workSheet  = wb.ActiveSheet :?> Worksheet
            
            //Range excelCellrange;
            //// now we resize the columns  
            let excelCellrange = workSheet.Range(workSheet.Cells.[1, 1], workSheet.Cells.[rowsCnt+ 1, colsCnt]);
            DisableCellStyle(excelCellrange) 
        
            for colIndex in [|0..colsCnt-1|] do
                workSheet.Cells.[1,colIndex+1] <- tbl.Columns.[colIndex].ColumnName
            //// rows
            for rowsIndex in [|0..rowsCnt-1|] do
                DoWork((int)(Convert.ToSingle(rowsIndex + 1) / (rowsCnt|>float32) * 100f));
                for colIndex in [|0..colsCnt-1|] do
                    workSheet.Cells.[rowsIndex + 2, colIndex + 1] <- tbl.Rows.[rowsIndex].[colIndex]
                    let cellText =tbl.Rows.[rowsIndex].[colIndex].ToString()
                    if(cellText = "" || (cellText.StartsWith("'") && cellText.StartsWith("'-")|>not))
                    then 
                        let excelCellrange = workSheet.Range(workSheet.Cells.[rowsIndex + 2, colIndex + 1], workSheet.Cells.[rowsIndex + 2, colIndex + 1])
                        EnableCellStyle(excelCellrange)

                    if(colIndex = 0 && (cellText = TextCommand||cellText = TextObserve))
                    then CellMerge(workSheet.Range(workSheet.Cells.[rowsIndex + 2, colIndex + 6], workSheet.Cells.[rowsIndex + 2, colIndex + 9]))

            workSheet.Range(workSheet.Cells.[1, 1], workSheet.Cells.[rowsCnt + 1, colsCnt]) |> AutoFitNFilterColumn
        
            workSheet.SaveAs(excelFilePath)
            excelApp.Quit()
            DoWork(0)
            Console.WriteLine("Excel file saved!")

    
    


