// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Drawing
open System.Reflection
open Dual.Common.Core.FS
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

        let rowItems(dev:TaskDev, job:Job option) =
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
                            yield rowItems(dev, Some job) //첫 TaskDev만 만듬
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
            row.ItemArray <- Enum.GetNames(typedefof<IOColumn>).Select(fun f-> "" |> box).ToArray()
            row |> dt.Rows.Add |>ignore

        let toBtnText(btns:ButtonDef seq, xlsCase:ExcelCase) =
            for btn in  btns do
                let func = btn.Funcs |> funcToText
                dt.Rows.Add(xlsCase.ToText(),  btn.Name   , "bool",  btn.InAddress,    btn.OutAddress,  "",  func) |> ignore
        let toLampText(lamps:LampDef seq, xlsCase:ExcelCase) =
            for lamp in  lamps do
                let func = lamp.Funcs |> funcToText
                dt.Rows.Add(xlsCase.ToText(),  lamp.Name  , "bool",  "",  lamp.OutAddress,  "",  func) |> ignore

        let toCondiText(conds:ConditionDef seq, xlsCase:ExcelCase) =
            for cond in  conds do
                let func = cond.Funcs |> funcToText
                dt.Rows.Add(xlsCase.ToText(),  cond.Name  , "bool",  cond.InAddress, "",   "",  func) |> ignore

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

        dt.Rows.Add(TextXlsVariable,  ""  ,  ""  , "", "","","") |> ignore
        emptyLine()
        dt

    let ToDataSet(system:DsSystem) = ToTable system
