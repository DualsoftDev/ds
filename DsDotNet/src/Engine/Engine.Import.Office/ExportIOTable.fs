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


    let addRows rows (dt:DataTable) = 
        rows
        |> Seq.iter (fun row ->
            let rowTemp = dt.NewRow()
            rowTemp.ItemArray <- (row |> Seq.cast<obj> |> Seq.toArray)
            dt.Rows.Add(rowTemp) |> ignore)

    let emptyRow (cols:string array) (dt:DataTable) =
        let row = dt.NewRow()
        row.ItemArray <- cols.Select(fun f -> "" |> box).ToArray()
        row |> dt.Rows.Add |> ignore

    let ToTable (sys: DsSystem) (selectFlows:Flow seq) (containSys:bool) : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name}")
        dt.Columns.Add($"{IOColumn.Case}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Input}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Output}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Func}", typeof<string>)  |> ignore

      
        let  addHeaderLine() =
            let  rowHeaderItems =
                [
                    $"{IOColumn.Case}"
                    $"{IOColumn.Name}"
                    $"{IOColumn.DataType}"
                    $"{IOColumn.Input}"
                    $"{IOColumn.Output}" 
                    $"{IOColumn.Func}"
                ]
            let row = dt.NewRow()
            row.ItemArray <- rowHeaderItems.Select(fun f -> f |> box).ToArray()
            row |> dt.Rows.Add |> ignore


        let rowItems (dev: TaskDev, job: Job, firstJobRow :bool) =
            let funcs =
                if firstJobRow then
                    if job.Func.IsSome then job.Func.Value.ToDsText() else ""
                else
                    TextFuncNotUsed

            let inSkip, outSkip =
                match job.ActionType with
                |NoneRx -> true,false
                |NoneTx -> false,true
                |NoneTRx -> true,true
                |_ ->  false,false


            [ TextXlsAddress
              dev.ApiName
              "bool"
              getValidAddress(dev.InAddress,  dev.QualifiedName, inSkip,  IOType.In)
              getValidAddress(dev.OutAddress, dev.QualifiedName, outSkip, IOType.Out)
              funcs ]

        let rows =
            let calls = selectFlows.SelectMany(fun f-> f.GetVerticesOfFlow().OfType<Call>())
            let jobs = calls.Select(fun c->c.TargetJob)
            seq {

                let devJobSet = sys.Jobs.SelectMany(fun j-> j.DeviceDefs.Select(fun dev-> dev,j))

                for (dev, job) in devJobSet |> Seq.sortBy (fun (dev,j) ->dev.ApiName) do
                    if jobs.Contains job
                    then 
                        let sortedDeviceDefs = job.DeviceDefs |> Seq.sortBy (fun f -> f.ApiName)
                        if sortedDeviceDefs.Head() = dev then
                            yield rowItems (dev, job, true) //첫 TaskDev만 만듬
                        else
                            yield rowItems (dev, job, false)
            }
        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<IOColumn>)) dt

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
                    let i= getValidCondiAddress(cond)
                    dt.Rows.Add(xlsCase.ToText(), cond.Name, "bool",i, TextSkip  ,  func)
                    |> ignore

        emptyLine ()
        toCondiText (sys.ReadyConditions, ExcelCase.XlsConditionReady)


        addHeaderLine()
        toBtnText (sys.AutoHWButtons, ExcelCase.XlsAutoBTN)
        toBtnText (sys.ManualHWButtons, ExcelCase.XlsManualBTN)
        toBtnText (sys.DriveHWButtons, ExcelCase.XlsDriveBTN)
        toBtnText (sys.PauseHWButtons, ExcelCase.XlsPauseBTN)
        toBtnText (sys.ClearHWButtons, ExcelCase.XlsClearBTN)
        toBtnText (sys.EmergencyHWButtons, ExcelCase.XlsEmergencyBTN)
        toBtnText (sys.HomeHWButtons, ExcelCase.XlsHomeBTN)
        toBtnText (sys.TestHWButtons, ExcelCase.XlsTestBTN)
        toBtnText (sys.ReadyHWButtons, ExcelCase.XlsReadyBTN)
        emptyLine ()
        toLampText (sys.AutoHWLamps, ExcelCase.XlsAutoLamp)
        toLampText (sys.ManualHWLamps, ExcelCase.XlsManualLamp)
        toLampText (sys.IdleHWLamps, ExcelCase.XlsIdleLamp)
        toLampText (sys.ErrorHWLamps, ExcelCase.XlsErrorLamp)
        toLampText (sys.OriginHWLamps, ExcelCase.XlsHomingLamp)
        toLampText (sys.ReadyHWLamps, ExcelCase.XlsReadyLamp)
        toLampText (sys.DriveHWLamps, ExcelCase.XlsDriveLamp)
        toLampText (sys.TestHWLamps, ExcelCase.XlsTestLamp)

        emptyLine ()
        dt.Rows.Add(TextXlsVariable) |> ignore
        emptyLine ()

        dt

    let ToDevicesTable (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name}_Devices")
        dt.Columns.Add($"{DeviceColumn.No}", typeof<string>) |> ignore
        dt.Columns.Add($"{DeviceColumn.Name}", typeof<string>) |> ignore
      
        let rowItems (no:int, dev: string) =
            [ 
              $"Dev{no+1}"
              dev
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                           .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
                    
            let devs  = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev))
                                    |> Seq.map (fun dev -> dev.DeviceName) 
                                    |> distinct
                                    |> Seq.sortBy (id) 
            devs
                |> Seq.mapi (fun i dev ->
                                    rowItems (i, dev) 
                            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<DeviceColumn>)) dt
     
        emptyLine ()
        dt

    let ToManualTable_IN (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name}_디바이스 센서")
        dt.Columns.Add($"{ManualColumn_I.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_I.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_I.Input}", typeof<string>) |> ignore
      
        let rowItems (no:int, dev: TaskDev, call:Call) =
            [ 
              dev.ApiName
              "bool"
              if dev.InAddress = TextSkip then "%HX0" else dev.InAddress
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
            let devCallSet = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev,c))
                                    |> Seq.sortBy (fun (dev, c) -> dev.ApiName)
            devCallSet
                |> Seq.mapi (fun i (dev, call) ->
                                    rowItems (i, dev, call) 
                            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_I>)) dt

     
        emptyLine ()
        dt

    let ToManualTable_OUT (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name}_디바이스 출력(Q)")
        dt.Columns.Add($"{ManualColumn_O.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_O.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_O.Output}", typeof<string>) |> ignore

      
        let rowItems (no:int, dev: TaskDev, call:Call) =
            [ 
              dev.ApiName
              "bool"
              if dev.OutAddress = TextSkip then "%HX0" else dev.OutAddress
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
            let devCallSet = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev,c))
                                    |> Seq.sortBy (fun (dev, c) -> dev.ApiName)
            devCallSet
                |> Seq.mapi (fun i (dev, call) ->
                                    rowItems (i, dev, call) 
                            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_O>)) dt

     
        emptyLine ()
        dt

    let ToManualTable_Memory (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name}_디바이스 명령(M)")
        dt.Columns.Add($"{ManualColumn_M.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_M.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_M.Manual}", typeof<string>) |> ignore

      
        let rowItems (no:int, dev: TaskDev, call:Call) =
            [ 
              dev.ApiName
              "bool"
              call.ManualTag.Address
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
            let devCallSet = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev,c))
                                    |> Seq.sortBy (fun (dev, c) -> dev.ApiName)
            devCallSet
                |> Seq.mapi (fun i (dev, call) ->
                                    rowItems (i, dev, call) 
                            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_M>)) dt

     
        emptyLine ()
        dt




    let ToAlramTable (sys: DsSystem)  : DataTable =
        let mutable no = 0
        let dt = new System.Data.DataTable($"{sys.Name}_AlramTable")
        dt.Columns.Add($"{AlramColumn.No}", typeof<string>) |> ignore
        dt.Columns.Add($"{AlramColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{AlramColumn.ErrorAddress}", typeof<string>) |> ignore

        let rowItems (name:string, address :string) =
            no <- no+1
            [ 
              $"Alram{no}"
              name
              address
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
                        
            seq {
                //1. call 부터
                for call in calls |> Seq.sortBy (fun c -> c.Name) do
                    yield rowItems ($"{call.Name}_센서쇼트이상", call.ErrorSensorOn.Address)
                    yield rowItems ($"{call.Name}_센서단락이상", call.ErrorSensorOff.Address)
                    yield rowItems ($"{call.Name}_시간초과이상", call.ErrorTimeOver.Address)
                    yield rowItems ($"{call.Name}_시간부족이상", call.ErrorTimeShortage.Address)

                //2. emg step
                for emg in sys.HWButtons.Where(fun f-> f.ButtonType = DuEmergencyBTN) do
                    yield rowItems ($"{emg.Name}_버튼눌림", emg.ErrorEmergency.Address)

                //3 . HWConditions step
                for condi in sys.HWConditions do
                    yield rowItems ($"{condi.Name}_조건이상", condi.ErrorCondition.Address)
            }

        addRows rows dt

        let emptyLine () = emptyRow (Enum.GetNames(typedefof<AlramColumn>)) dt
     
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
        static member ExportIOListToExcel (system: DsSystem) (filePath: string) =
            let dataTables = [|ToIOListDataSet system|]
            createSpreadsheet filePath dataTables 25.0 true

        [<Extension>]
        static member ExportHMITableToExcel (system: DsSystem) (filePath: string) =
            let dataTables = [|ToManualTable_IN system;ToManualTable_OUT system;ToManualTable_Memory system; ToDevicesTable system;ToAlramTable system|]
            createSpreadsheet filePath dataTables 25.0 false

        [<Extension>]
        static member ToDataCSVFlows  (system: DsSystem) (flowNames:string seq) (conatinSys:bool)  =
            let dataTable = ToTable system (system.Flows.Where(fun f->flowNames.Contains(f.Name))) conatinSys
            ToDataTableToCSV dataTable "IOTABLE"

        [<Extension>]
        static member ToDataCSVLayouts (xs: Flow seq) =
            let dataTable = ToLayoutTable xs 
            ToDataTableToCSV dataTable "LAYOUT"
      