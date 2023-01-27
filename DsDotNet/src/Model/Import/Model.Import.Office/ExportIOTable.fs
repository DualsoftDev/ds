// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
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
        dt.Columns.Add($"{IOColumn.DataType}"   , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Input}"      , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Output}"     , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Job}"        , typeof<string>) |>ignore
        dt.Columns.Add($"{IOColumn.Func}"       , typeof<string>) |>ignore

        let funcToText(xs:Func seq) = xs.Select(fun f->f.ToDsText()).JoinWith(";")

        let rowItems(dev:TaskDevice, job:Job option) =
            let jobName, funcs =
                if job.IsSome
                then job.Value.Name
                     , job.Value.Funcs.Cast<Func>() |> funcToText
                else "↑", "↑"

            [TextXlsAddress;  dev.ApiName; "bool"; dev.InAddress  ; dev.OutAddress; jobName; funcs; ]

        let rows =
            seq {
                for job in sys.Jobs |> Seq.sortBy(fun f->f.Name) do
                    let sortedDeviceDefs = job.DeviceDefs |> Seq.sortBy(fun f->f.ApiName)

                    for dev in  sortedDeviceDefs do
                        if sortedDeviceDefs.Head() = dev
                        then
                            yield rowItems(dev, Some job) //첫 TaskDevice만 만듬
                        else
                            yield rowItems(dev, None)
            }

        rows
        |> Seq.iter(fun row ->
                    let rowTemp = dt.NewRow()
                    rowTemp.ItemArray <- (row|> Seq.cast<obj>|> Seq.toArray)
                    dt.Rows.Add(rowTemp) |> ignore)

        let emptyLine() =
            let row = dt.NewRow()
            row.ItemArray <- Enum.GetNames(typedefof<IOColumn>).Select(fun f-> "'-" |> box).ToArray()
            row |> dt.Rows.Add |>ignore

        let toBtnText(btns:ButtonDef seq, xlsCase:ExcelCase) =
            for btn in  btns do
                let func = btn.Funcs |> funcToText
                dt.Rows.Add(xlsCase.ToText(),  btn.Name   , "bool",  btn.InAddress,    btn.OutAddress,  "'-",  func) |> ignore
        let toLampText(lamps:LampDef seq, xlsCase:ExcelCase) =
            for lamp in  lamps do
                let func = lamp.Funcs |> funcToText
                dt.Rows.Add(xlsCase.ToText(),  lamp.Name  , "bool",  "'-",  lamp.OutAddress,  "'-",  func) |> ignore

        let toCondiText(conds:ConditionDef seq, xlsCase:ExcelCase) =
            for cond in  conds do
                let func = cond.Funcs |> funcToText
                dt.Rows.Add(xlsCase.ToText(),  cond.Name  , "bool",  cond.InAddress, "'-",   "'-",  func) |> ignore

        emptyLine()
        emptyLine()

        toBtnText (sys.AutoButtons, ExcelCase.XlsAutoBTN)
        toBtnText (sys.ManualButtons, ExcelCase.XlsManualBTN)
        toBtnText (sys.DriveButtons, ExcelCase.XlsDriveBTN)
        toBtnText (sys.StopButtons, ExcelCase.XlsStopBTN)
        toBtnText (sys.EmergencyButtons, ExcelCase.XlsEmergencyBTN)
        toBtnText (sys.TestButtons, ExcelCase.XlsTestBTN)
        toBtnText (sys.ReadyButtons, ExcelCase.XlsReadyBTN)
        toBtnText (sys.ClearButtons, ExcelCase.XlsClearBTN)
        toBtnText (sys.HomeButtons, ExcelCase.XlsHomeBTN)

        emptyLine()
        emptyLine()

        toLampText (sys.DriveLamps, ExcelCase.XlsDriveLamp)
        toLampText (sys.AutoLamps, ExcelCase.XlsAutoLamp)
        toLampText (sys.ManualLamps, ExcelCase.XlsManualLamp)
        toLampText (sys.TestLamps, ExcelCase.XlsTestLamp)
        toLampText (sys.StopLamps, ExcelCase.XlsStopLamp)
        toLampText (sys.ReadyLamps, ExcelCase.XlsReadyLamp)
        toLampText (sys.IdleLamps, ExcelCase.XlsIdleLamp)
        toLampText (sys.EmergencyLamps, ExcelCase.XlsEmergencyLamp)

        emptyLine()
        emptyLine()

        toCondiText (sys.ReadyConditions, ExcelCase.XlsConditionReady)
        toCondiText (sys.DriveConditions, ExcelCase.XlsConditionDrive)

        dt.Rows.Add(TextXlsVariable,  ""  ,  ""  , "'-", "'-","'-","'-") |> ignore
        emptyLine()
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

                    //if(colIndex = 0)
                    //then cellMerge(workSheet.Range(workSheet.Cells.[rowsIndex + 2, colIndex + 6], workSheet.Cells.[rowsIndex + 2, colIndex + 9]))

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





