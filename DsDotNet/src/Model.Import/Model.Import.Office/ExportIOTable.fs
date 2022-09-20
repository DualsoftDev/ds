// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open Microsoft.Office.Interop
open Microsoft.Office.Interop.Excel
open System
open System.Drawing
open System.Reflection
open System.Data.OleDb
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module ExportIOTable =

    let ToTable(model:ImportModel) =

        let dt = new System.Data.DataTable($"{model.Name}")
        dt.Columns.Add("Case", typeof<obj>) |>ignore
        dt.Columns.Add("MFlow", typeof<string>) |>ignore
        dt.Columns.Add("Name", typeof<string>) |>ignore
        dt.Columns.Add("Type", typeof<string>) |>ignore
        dt.Columns.Add("Size", typeof<string>) |>ignore
        dt.Columns.Add("S(Output)", typeof<string>) |>ignore
        dt.Columns.Add("R(Output)", typeof<string>) |>ignore
        dt.Columns.Add("E(Input)" , typeof<string>) |>ignore


        let rowItems(causal:NodeType, seg:MSeg, trx:string) =
            let MFlowName, name =  seg.OwnerMFlow, seg.Name
            match causal with
            |TR ->  ["주소"; MFlowName; name; trx; "bit"; seg.TextStart; "'-"          ; seg.TextEnd]
            |TX  -> ["주소"; MFlowName; name; trx; "bit"; seg.TextStart; "'-"          ; "'-"]
            |RX  -> ["주소"; MFlowName; name; trx; "bit"; "'-"         ; "'-"          ; seg.TextEnd]
            |EX  -> ["주소"; MFlowName; name; trx; "bit"; seg.TextStart; seg.TextReset ; seg.TextEnd]
            |_ -> failwithf "ERR";

        let rows =
            seq {
                for sys in  model.Systems do
                    let flows = sys.RootFlows() |> Seq.cast<MFlow> |> Seq.filter(fun flow -> (flow.Page = Int32.MaxValue)|>not)
                    //MFlow 출력
                    for flow in flows do
                        //Call Task 출력
                        for callSeg in flow.CallSegs() do
                            for index in [|1..callSeg.MaxCnt|] do
                                let causal, trx = callSeg.PrintfTRX(index, true)
                                yield rowItems(causal, callSeg, trx)
                        //Ex Task 출력
                        for callSeg in flow.ExRealSegs() do
                            yield rowItems(EX, callSeg, "EX")
            }
        rows
        |> Seq.iter(fun row -> 
                    let rowTemp = dt.NewRow()
                    rowTemp.ItemArray <- (row|> Seq.cast<obj>|> Seq.toArray)
                    dt.Rows.Add(rowTemp) |> ignore)

        dt.Rows.Add("내부", "변수", ""  , "'-", ""  , "'-", "'-", "'-") |> ignore
        dt.Rows.Add("지시", "함수", ""  , "'-", "'-", ""  , "'-", "'-") |> ignore
        dt.Rows.Add("관찰", "함수", ""  , "'-", "'-", "'-", "'-", ""  ) |> ignore

        for sys in  model.Systems do
            let sys = sys :?> MSys
            for btn in  sys.EmgSet do
                dt.Rows.Add("버튼", "비상", btn.Key  , "'-", "'-", "'-", "'-", ""  ) |> ignore
            for btn in  sys.AutoSet do
                dt.Rows.Add("버튼", "자동", btn.Key  , "'-", "'-", "'-", "'-", ""  ) |> ignore
            for btn in  sys.StartSet do
                dt.Rows.Add("버튼", "시작", btn.Key  , "'-", "'-", "'-", "'-", ""  ) |> ignore
            for btn in  sys.ResetSet do
                dt.Rows.Add("버튼", "리셋", btn.Key  , "'-", "'-", "'-", "'-", ""  ) |> ignore
        dt

 


    let ToFiie(model:ImportModel, excelFilePath:string) = 
        let DisableCellStyle(range:Microsoft.Office.Interop.Excel.Range ) =
            range.Interior.Color <- Color.LightGray;
            range.Font.Bold <- true;
            range.Borders.LineStyle <- XlLineStyle.xlContinuous;
            range.Borders.Weight <- 2;

        let EnableCellStyle(range:Microsoft.Office.Interop.Excel.Range ) =
                   range.Interior.Color <- Color.LightYellow;
                   range.Borders.LineStyle <- XlLineStyle.xlDash;

        let AutoFitNFilterColumn(range:Microsoft.Office.Interop.Excel.Range ) =
                   range.AutoFilter(1, Type.Missing, XlAutoFilterOperator.xlAnd, Type.Missing, true) |> ignore
                   range.EntireColumn.AutoFit() |> ignore


        let tbl = ToTable(model:ImportModel)
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
                    
                    if(tbl.Rows.[rowsIndex].[colIndex].ToString() = "")
                    then 
                        let excelCellrange = workSheet.Range(workSheet.Cells.[rowsIndex + 2, colIndex + 1], workSheet.Cells.[rowsIndex + 2, colIndex + 1]);
                        EnableCellStyle(excelCellrange)

            workSheet.Range(workSheet.Cells.[1, 1], workSheet.Cells.[rowsCnt + 1, colsCnt]) |> AutoFitNFilterColumn
        
            workSheet.SaveAs(excelFilePath)
            excelApp.Quit()
            DoWork(0)
            Console.WriteLine("Excel file saved!")

    
    


