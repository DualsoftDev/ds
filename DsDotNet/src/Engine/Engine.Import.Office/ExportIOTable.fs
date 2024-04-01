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
            let coins = sys.GetVerticesOfCoinCalls()
            let jobs = calls.Select(fun c->c.TargetJob)
            seq {

                let devJobSet = sys.Jobs.SelectMany(fun j-> j.DeviceDefs.Select(fun dev-> dev,j))

                for (dev, job) in devJobSet |> Seq.sortBy (fun (dev,j) ->dev.ApiName) do
                    if not(coins.Where(fun c->c.TaskDevs.Contains(dev)).any())
                    then
                        dev.InAddress <- ExternalTempMemory
                        dev.OutAddress <- TextSkip
                        
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

    let getErrorRows(sys:DsSystem) =
        
        let mutable no = 0
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
                //1. call, real 부터
                for call in calls |> Seq.sortBy (fun c -> c.Name) do
                    yield rowItems ($"{call.Name}_센서쇼트이상", call.ErrorSensorOn.Address)
                    yield rowItems ($"{call.Name}_센서단락이상", call.ErrorSensorOff.Address)
                    yield rowItems ($"{call.Name}_시간초과이상", call.ErrorTimeOver.Address)
                    yield rowItems ($"{call.Name}_시간부족이상", call.ErrorTimeShortage.Address)

                for real in sys.GetVertices().OfType<Real>() |> Seq.sortBy (fun r -> r.Name) do
                    yield rowItems ($"{real.Name}_작업원위치이상", real.ErrGoingOrigin.Address)

                //2. emg step
                for emg in sys.HWButtons.Where(fun f-> f.ButtonType = DuEmergencyBTN) do
                    yield rowItems ($"{emg.Name}_버튼눌림", emg.ErrorEmergency.Address)

                //3 . HWConditions step
                for condi in sys.HWConditions do
                    yield rowItems ($"{condi.Name}_조건이상", condi.ErrorCondition.Address)
            }
        rows

    let ToErrorTable (sys: DsSystem)  : DataTable =
        let dt = new System.Data.DataTable($"AlramTable")
        dt.Columns.Add($"{ErrorColumn.No}", typeof<string>) |> ignore
        dt.Columns.Add($"{ErrorColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ErrorColumn.ErrorAddress}", typeof<string>) |> ignore

        let rows = getErrorRows(sys)

        addRows rows dt

        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ErrorColumn>)) dt
     
        emptyLine ()
        dt

    let ToAlramTable (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"알람 리스트")
        dt.Columns.Add($"{TextColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty1}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty2}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty3}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Color}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Ltalic}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.UnderLine}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.StrikeOut}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Bold}", typeof<string>) |> ignore
      
        let rowItems (name: string) =
            [ 
              name
              ""
              ""
              ""
              "0"
              "Off"
              "Off"
              "Off"
              "On"
               ]


        let alramList = getErrorRows(sys)
        let rows= 
            alramList
            |> Seq.map (fun err ->
                                rowItems (err[ErrorColumn.Name|>int]) 
                        )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt

    let ToDevicesTable (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"디바이스이름")
        dt.Columns.Add($"{TextColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty1}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty2}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty3}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Color}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Ltalic}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.UnderLine}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.StrikeOut}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Bold}", typeof<string>) |> ignore
      
        let rowItems (no:int, dev: string) =
            [ 
              dev
              ""
              ""
              ""
              "16777215"
              "Off"
              "Off"
              "Off"
              "On"
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
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt

    let ToManualTable_IN (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"센서(I)")
        dt.Columns.Add($"{ManualColumn_I.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_I.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_I.Input}", typeof<string>) |> ignore
      
        let rowItems (dev: TaskDev, addr:string) =
            [ 
              dev.ApiName
              "bool"
              addr 
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
            let devCallSet = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev,c))
                                    |> Seq.sortBy (fun (dev, c) -> dev.ApiName)
            devCallSet
                |> Seq.collect (fun (dev, call) ->
                    [
                        yield rowItems (dev, if dev.InAddress = TextSkip then "%HX0" else dev.InAddress) 
                        if dev.ApiItem.ApiSystem.ApiItems.Count = 1
                            then 
                                yield rowItems (dev, "%HX0") 

                    ]
            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_I>)) dt

     
        emptyLine ()
        dt

    let ToManualTable_OUT (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"출력(Q)")
        dt.Columns.Add($"{ManualColumn_O.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_O.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_O.Output}", typeof<string>) |> ignore

      
        let rowItems (dev: TaskDev, addr:string) =
            [ 
              dev.ApiName
              "bool"
              addr
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
            let devCallSet = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev,c))
                                    |> Seq.sortBy (fun (dev, c) -> dev.ApiName)
            devCallSet
                |> Seq.collect (fun  (dev, call) ->
                            [
                                yield rowItems (dev, if dev.OutAddress = TextSkip then "%HX0" else dev.OutAddress) 
                                
                                if dev.ApiItem.ApiSystem.ApiItems.Count = 1
                                then 
                                    yield rowItems (dev, "%HX0") 
                            ]
                )




        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_O>)) dt

     
        emptyLine ()
        dt

    let ToManualTable_Memory (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"명령(M)")
        dt.Columns.Add($"{ManualColumn_M.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_M.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_M.Manual}", typeof<string>) |> ignore

      
        let rowItems ( dev: TaskDev, addr:string) =
            [ 
              dev.ApiName
              "bool"
              addr
               ]

        let rows =
            let calls = sys.GetVerticesOfCoins().OfType<Call>()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
            let devCallSet = calls.SelectMany(fun c-> c.TargetJob.DeviceDefs.Select(fun dev-> dev,c))
                                    |> Seq.sortBy (fun (dev, c) -> dev.ApiName)
            devCallSet
                |> Seq.collect (fun (dev, call) ->
                    [   
                        yield rowItems (dev, call.ManualTag.Address)

                        if dev.ApiItem.ApiSystem.ApiItems.Count = 1
                        then 
                            yield rowItems (dev, "%HX0") 

                    ]
                ) 

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_M>)) dt

     
        emptyLine ()
        dt

    let ToManualTable_BtnLamp (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"조작반(M)")
        dt.Columns.Add($"{ManualColumn_ControlPanel.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.Manual}", typeof<string>) |> ignore
      
        let rowItems (name  : string, address :string) =
            [ 
              name
              "bool"
              address
               ]

        let rows =
            let hws = sys.HWSystemDefs.Where(fun f->f :? ButtonDef || f :? LampDef )
                                      .Select(fun f-> f.Name, if f :? ButtonDef  then f.InAddress else f.OutAddress)
                                      .OrderBy(fun (name, addr) -> addr)
            hws
                |> Seq.map (fun (name, addr)  -> 
                    rowItems (name, addr)
                    )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn_M>)) dt

     
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
            let dataTables = [|ToIOListDataSet system; ToErrorTable system|]
            createSpreadsheet filePath dataTables 25.0 true

        [<Extension>]
        static member ExportHMITableToExcel (system: DsSystem) (filePath: string) =
            let dataTables = [|
                                ToManualTable_Memory system;
                                ToManualTable_IN system;
                                ToManualTable_OUT system;
                                ToManualTable_BtnLamp system;
                                ToDevicesTable system;
                                ToAlramTable system
                                |]
            createSpreadsheet filePath dataTables 25.0 false

        [<Extension>]
        static member ToDataCSVFlows  (system: DsSystem) (flowNames:string seq) (conatinSys:bool)  =
            let dataTable = ToTable system (system.Flows.Where(fun f->flowNames.Contains(f.Name))) conatinSys
            ToDataTableToCSV dataTable "IOTABLE"

        [<Extension>]
        static member ToDataCSVLayouts (xs: Flow seq) =
            let dataTable = ToLayoutTable xs 
            ToDataTableToCSV dataTable "LAYOUT"
      