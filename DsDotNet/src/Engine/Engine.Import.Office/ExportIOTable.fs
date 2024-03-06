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
module ExportIOTable =

    //let applyIfBtnLampSkip addr:string  = if addr = TextAddrEmpty then TextSkip else addr

    let ToTable (sys: DsSystem) (selectFlows:Flow seq) (containSys:bool) : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name}")
        dt.Columns.Add($"{IOColumn.Case}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Input}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Output}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Func}", typeof<string>)  |> ignore

      
        let rowItems (dev: TaskDev, job: Job option) =
            let funcs =
                if job.IsSome then
                    if job.Value.Func.IsSome then job.Value.Func.Value.ToDsText() else ""
                else
                    "↑"

            [ TextXlsAddress
              dev.ApiName
              "bool"
              getValidAddress(dev.InAddress,  dev.QualifiedName, dev.ApiItem.RXs.Count = 0, true)
              getValidAddress(dev.OutAddress, dev.QualifiedName, dev.ApiItem.TXs.Count = 0, false)
              funcs ]

        let rows =
            let jobs = selectFlows.SelectMany(fun f-> f.GetVerticesOfFlow().OfType<Call>()
                                                       .Select(fun c->c.TargetJob))
            seq {
                for job in sys.Jobs |> Seq.sortBy (fun f -> f.Name) do
                    if jobs.Contains job
                    then 
                        let sortedDeviceDefs = job.DeviceDefs |> Seq.sortBy (fun f -> f.ApiName)

                        for dev in sortedDeviceDefs do
                            if sortedDeviceDefs.Head() = dev then
                                yield rowItems (dev, Some job) //첫 TaskDev만 만듬
                            else
                                yield rowItems (dev, None)
            }

        rows
        |> Seq.iter (fun row ->
            let rowTemp = dt.NewRow()
            rowTemp.ItemArray <- (row |> Seq.cast<obj> |> Seq.toArray)
            dt.Rows.Add(rowTemp) |> ignore)

        let emptyLine () =
            let row = dt.NewRow()
            row.ItemArray <- Enum.GetNames(typedefof<IOColumn>).Select(fun f -> "" |> box).ToArray()
            row |> dt.Rows.Add |> ignore

        let toBtnText (btns: ButtonDef seq, xlsCase: ExcelCase) =
            for btn in btns do
                if containSys then
                    let func =  if btn.Func.IsSome then btn.Func.Value.ToDsText() else ""
                    let i, o = getValidBtnAddress(btn)
                    dt.Rows.Add(xlsCase.ToText(), btn.Name, "bool",  i, o ,func)
                    |> ignore

        let toLampText (lamps: LampDef seq, xlsCase: ExcelCase) =
            for lamp in lamps do
                if containSys then
                    let func =  if lamp.Func.IsSome then lamp.Func.Value.ToDsText() else ""

                    let i, o = getValidLampAddress(lamp)
                    dt.Rows.Add(xlsCase.ToText(), lamp.Name, "bool",   i, o, func)
                    |> ignore

        let toCondiText (conds: ConditionDef seq, xlsCase: ExcelCase) =
            for cond in conds do
                if containSys then
                    let func =  if cond.Func.IsSome then cond.Func.Value.ToDsText() else ""
                    let i, o = getValidCondiAddress(cond)
                    dt.Rows.Add(xlsCase.ToText(), cond.Name, "bool",i, o  ,  func)
                    |> ignore

        emptyLine ()
        emptyLine ()

        toBtnText (sys.AutoHWButtons, ExcelCase.XlsAutoBTN)
        toBtnText (sys.ManualHWButtons, ExcelCase.XlsManualBTN)
        toBtnText (sys.DriveHWButtons, ExcelCase.XlsDriveBTN)
        toBtnText (sys.PauseHWButtons, ExcelCase.XlsPauseBTN)
        toBtnText (sys.EmergencyHWButtons, ExcelCase.XlsEmergencyBTN)
        toBtnText (sys.TestHWButtons, ExcelCase.XlsTestBTN)
        toBtnText (sys.ReadyHWButtons, ExcelCase.XlsReadyBTN)
        toBtnText (sys.ClearHWButtons, ExcelCase.XlsClearBTN)
        toBtnText (sys.HomeHWButtons, ExcelCase.XlsHomeBTN)

        emptyLine ()
        emptyLine ()

        toLampText (sys.AutoHWLamps, ExcelCase.XlsAutoLamp)
        toLampText (sys.ManualHWLamps, ExcelCase.XlsManualLamp)
        toLampText (sys.DriveHWLamps, ExcelCase.XlsDriveLamp)
        toLampText (sys.ErrorHWLamps, ExcelCase.XlsErrorLamp)
        toLampText (sys.TestHWLamps, ExcelCase.XlsTestLamp)
        toLampText (sys.ReadyHWLamps, ExcelCase.XlsReadyLamp)
        toLampText (sys.IdleHWLamps, ExcelCase.XlsIdleLamp)
        toLampText (sys.OriginHWLamps, ExcelCase.XlsHomingLamp)

        emptyLine ()
        emptyLine ()

        toCondiText (sys.ReadyConditions, ExcelCase.XlsConditionReady)
        
        dt.Rows.Add(TextXlsVariable) |> ignore
        emptyLine ()
        dt

    let ToIOListDataSet (system: DsSystem)  = 
        let table = ToTable system system.Flows true
        table.Columns.Remove($"{IOColumn.Case}")
        table.Columns.Remove($"{IOColumn.Func}")
        table


    let ToDataTableToCSV  (dataTable: DataTable) (fileName:string)  =
        let csvContent = new StringBuilder()
        // 컬럼 헤더 추가
        let columnNames =
            dataTable.Columns |> Seq.cast<DataColumn> |> Seq.map (fun col -> col.ColumnName)

        csvContent.AppendLine(String.Join("\t", columnNames |> Seq.toArray)) |> ignore

        // 각 행의 데이터 추가
        dataTable.Rows
        |> Seq.cast<DataRow>
        |> Seq.iter (fun row ->
            let fieldValues =
                row.ItemArray
                |> Seq.map (fun obj ->
                    let field = obj.ToString()

                    field.Replace("\t", "\\t") // 새 줄 문자 처리
                )

            csvContent.AppendLine(String.Join("\t", fieldValues |> Seq.toArray)) |> ignore)

        // 임시 파일 경로를 생성
        let tempFilePath = Path.Combine(Path.GetTempPath(), $"{fileName}.csv")
        // CSV 내용을 파일에 씀
        File.WriteAllText(tempFilePath, csvContent.ToString())
        tempFilePath



   

    [<Extension>]
    type OfficeExcelExt =
        [<Extension>]
        static member ExportDataTableToExcel (system: DsSystem) (filePath: string) =
            let dataTables = [|ToIOListDataSet system|]
            createSpreadsheet filePath dataTables 40.0
        [<Extension>]
        static member ToDataCSVFlows  (system: DsSystem) (flowNames:string seq) (conatinSys:bool)  =
            let dataTable = ToTable system (system.Flows.Where(fun f->flowNames.Contains(f.Name))) conatinSys
            ToDataTableToCSV dataTable "IOTABLE"
        [<Extension>]
        static member ToDataCSVLayouts (xs: Flow seq) =
            let dataTable = ToLayoutTable xs 
            ToDataTableToCSV dataTable "LAYOUT"
      