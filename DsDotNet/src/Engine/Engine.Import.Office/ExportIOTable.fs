// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.IO
open System.Text
open System.Data
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open Newtonsoft.Json
open System.Collections.Generic


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


    let  addIOColumn(dt:DataTable) =

        dt.Columns.Add($"{IOColumn.Case}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Flow}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.DataType}", typeof<string>) |> ignore
        dt.Columns[dt.Columns.Count-1].ColumnName <- "DataType \n(In:Out)"
        dt.Columns.Add($"{IOColumn.Input}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Output}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.InSymbol}", typeof<string>)  |> ignore
        dt.Columns[dt.Columns.Count-1].ColumnName <- "Symbol\nIn"
        dt.Columns.Add($"{IOColumn.OutSymbol}", typeof<string>)  |> ignore
        dt.Columns[dt.Columns.Count-1].ColumnName <- "Symbol\nOut"

    let emptyLine (dt:DataTable) = emptyRow (Enum.GetNames(typedefof<IOColumn>)) dt
    let getFlowExportName(hw:HwSystemDef)  =
        if hw.IsGlobalSystemHw 
            then "ALL" 
            else String.Join(";", hw.SettingFlows.Select(fun f->f.Name))

    let ToPanelIOTable(sys: DsSystem) (containSys:bool) target : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name} Panel IO LIST")
        
        addIOColumn dt

        let toBtnText (btns: ButtonDef seq, xlsCase: ExcelCase) =
            for btn in btns do
                if containSys then
                    updateHwAddress (btn) (btn.InAddress, btn.OutAddress) target
                    let dType = getPptHwDevDataTypeText btn
                    dt.Rows.Add(xlsCase.ToText(), getFlowExportName(btn), btn.Name, dType,  btn.InAddress, btn.OutAddress ,"", "")
                    |> ignore

        let toLampText (lamps: LampDef seq, xlsCase: ExcelCase) =
            for lamp in lamps do
                if containSys then
                    updateHwAddress (lamp) (lamp.InAddress, lamp.OutAddress) target
                    let dType = getPptHwDevDataTypeText lamp
                    dt.Rows.Add(xlsCase.ToText(), getFlowExportName(lamp), lamp.Name, dType,  lamp.InAddress, lamp.OutAddress ,"", "")
                    |> ignore


        toBtnText  (sys.AutoHWButtons,      ExcelCase.XlsAutoBTN)
        toBtnText  (sys.ManualHWButtons,    ExcelCase.XlsManualBTN)
        toBtnText  (sys.DriveHWButtons,     ExcelCase.XlsDriveBTN)
        toBtnText  (sys.PauseHWButtons,     ExcelCase.XlsPauseBTN)
        toBtnText  (sys.ClearHWButtons,     ExcelCase.XlsClearBTN)
        toBtnText  (sys.EmergencyHWButtons, ExcelCase.XlsEmergencyBTN)
        toBtnText  (sys.HomeHWButtons,      ExcelCase.XlsHomeBTN)
        toBtnText  (sys.TestHWButtons,      ExcelCase.XlsTestBTN)
        toBtnText  (sys.ReadyHWButtons,     ExcelCase.XlsReadyBTN)
        toLampText (sys.AutoHWLamps,        ExcelCase.XlsAutoLamp)
        toLampText (sys.ManualHWLamps,      ExcelCase.XlsManualLamp)
        toLampText (sys.IdleHWLamps,        ExcelCase.XlsIdleLamp)
        toLampText (sys.ErrorHWLamps,       ExcelCase.XlsErrorLamp)
        toLampText (sys.OriginHWLamps,      ExcelCase.XlsHomingLamp)
        toLampText (sys.ReadyHWLamps,       ExcelCase.XlsReadyLamp)
        toLampText (sys.DriveHWLamps,       ExcelCase.XlsDriveLamp)
        toLampText (sys.TestHWLamps,        ExcelCase.XlsTestLamp)


        dt

    let splitNameForRow(name:string) =
        let items = name.Split(TextDeviceSplit) //gpt에서 생성하면  TextDeviceSplit 규격 안따름
        if items.Length = 1 then
            "", items.[0]
        else
            let head = items[0];
            let tail = name[(head.Length+TextDeviceSplit.length())..]
            head, tail

    let rowIOItems (dev: TaskDev, job: Job) target =
        let inSym  =  dev.TaskDevParamIO.InParam.Symbol
        let outSym =  dev.TaskDevParamIO.OutParam.Symbol
        let inSkip, outSkip = dev.GetSkipInfo(job)

        let flow, name = splitNameForRow $"{dev.DeviceName}.{dev.ApiItem.PureName}"
        [
            TextXlsAddress
            flow
            name
            getPptDevDataTypeText (dev)
            getValidAddress(dev.InAddress,  dev.InDataType,  dev.QualifiedName, inSkip,  IOType.In,  target )
            getValidAddress(dev.OutAddress, dev.OutDataType, dev.QualifiedName, outSkip, IOType.Out, target )
            inSym
            outSym
        ]

    let IOchunkBySize = 22
    let ExcelchunkBySize = 1000000
    let PDFchunkBySize = 100


    let ToDeviceIOTables  (sys: DsSystem) (rowSize:int) target : DataTable seq =

        let totalRows =
            seq {
                let mutable extCnt = 0
                let devsJob =  sys.GetTaskDevsSkipEmptyAddress()

                for (dev, job) in  devsJob do
                    //외부입력 전용 확인하여 출력 생성하지 않는다.
                    if  dev.IsRootOnlyDevice
                    then
                        dev.OutAddress <- (TextSkip)
                        if dev.InAddress = TextAddrEmpty
                        then
                            dev.InAddress  <-  getExternalTempMemory (target, extCnt)
                            extCnt <- extCnt+1

                    yield rowIOItems (dev, job) target
        }

        let dts =
            totalRows
            |> Seq.chunkBySize(rowSize)
            |> Seq.mapi(fun i rows->
                let dt = new System.Data.DataTable($"{sys.Name} Device IO LIST {i+1}")
                addIOColumn dt
                addRows rows dt
                dt
            )

        dts

    type Statement with
        member x.ToConditionText() =
            match x with
            | DuAssign (_condition, expr, _) -> $"{expr.ToText()}"
            | DuVarDecl (expr, _) -> $"{expr.ToText()}"
            | DuTimer timerStatement ->
                let ts, t = timerStatement, timerStatement.Timer
                let functionName = ts.FunctionName  // e.g "createTON"
                let args = [    // [preset; rung-in-condition; (reset-condition)]
                    sprintf "%A" t.PRE.Value
                    match ts.RungInCondition with | Some c -> c.ToText() | None -> ()
                    match ts.ResetCondition  with | Some c -> c.ToText() | None -> () ]
                let args = String.Join(", ", args)
                $"{functionName}({args})"

            | DuCounter counterStatement ->
                let cs, c = counterStatement, counterStatement.Counter
                let functionName = cs.FunctionName  // e.g "createCTU"
                let args = [    // [preset; up-condition; (down-condition;) (reset-condition;) (accum;)]
                    sprintf "%A" c.PRE.Value
                    match cs.UpCondition    with | Some c -> c.ToText() | None -> ()
                    match cs.DownCondition  with | Some c -> c.ToText() | None -> ()
                    match cs.ResetCondition with | Some c -> c.ToText() | None -> ()
                    if c.ACC.Value <> 0u then
                        sprintf "%A" c.ACC.Value ]
                let args = String.Join(", ", args)
                $"{functionName}({args})"
            //| DuAction (DuCopy (condition, _, _)) -> $"{condition.ToText()}"
            | DuAction (DuCopyUdt {Condition=condition}) -> $"{condition.ToText()}"
            | DuPLCFunction _ ->
                failwithlog "ERROR"
            | (DuUdtDecl _ | DuUdtDef _) -> failwith "Unsupported.  Should not be called for these statements"
            | (DuLambdaDecl _ | DuProcDecl _ | DuProcCall _) ->
                failwith "ERROR: Not yet implemented"       // 추후 subroutine 사용시, 필요에 따라 세부 구현

    let ToFuncVariTables  (sys: DsSystem)  hwTarget: DataTable seq =


        let funcText (xs:Statement seq)=    String.Join(";", xs.Select(fun s->s.ToText()))
        let funcOperatorText (xs:Statement seq)=    String.Join(";", xs.Select(fun s->s.ToConditionText()))

        let operatorRows =

            sys.Functions
                .OfType<OperatorFunction>()
                                    .Map(fun func->
                                    let _flow, name = splitNameForRow func.Name
                                    let funcText =    funcOperatorText func.Statements


                                    [ TextXlsOperator
                                      TextXlsAllFlow
                                      name
                                      TextSkip
                                      funcText
                                      TextSkip
                                      TextSkip
                                      TextSkip
                                      ]
                                      )

        let commandRows =
            sys.Functions
                .OfType<CommandFunction>()
                                    .Map(fun func->
                                    let _flow, name = splitNameForRow func.Name
                                    let funcText =    funcText func.Statements


                                    [ TextXlsCommand
                                      TextXlsAllFlow
                                      name
                                      TextSkip
                                      TextSkip
                                      funcText
                                      TextSkip
                                      TextSkip
                                      ]
                                      )

        let variRows = sys.Variables.Map(fun vari->
                [
                  if vari.VariableType = Mutable then  TextXlsVariable else TextXlsConst
                  TextXlsAllFlow
                  vari.Name
                  vari.Type.ToText()
                  if vari.VariableType = Mutable then  TextSkip else vari.InitValue
                  TextSkip
                  TextSkip
                  TextSkip
                  ]
                  )

      

        let condiRows =
            let getXlsConditionLabel (conditionType:ConditionType) =
                match conditionType with
                | DuReadyState  -> TextXlsConditionReady
                | DuDriveState  -> TextXlsConditionDrive


            sys.HWConditions
            |> Seq.sortBy(fun cond -> cond.Name)
            |> Seq.map(fun cond ->
                let _, name = splitNameForRow cond.Name
                updateHwAddress (cond) (cond.InAddress, cond.OutAddress) hwTarget
                [
                    getXlsConditionLabel cond.ConditionType
                    getFlowExportName (cond)
                    name
                    getPptHwDevDataTypeText cond
                    cond.InAddress
                    cond.OutAddress
                    cond.TaskDevParamIO.InParam.Symbol
                    cond.TaskDevParamIO.OutParam.Symbol
                ]
            )
        let actionRows =
            let getXlsActionLabel (actionType:ActionType) =
                match actionType with
                | DuEmergencyAction  -> TextXlsActionEmg
                | DuPauseAction      -> TextXlsActionPause

            sys.HWActions
            |> Seq.sortBy(fun action -> action.Name)
            |> Seq.map(fun action ->
                let _, name = splitNameForRow action.Name
                updateHwAddress (action) (action.InAddress, action.OutAddress) hwTarget
                [
                    getXlsActionLabel action.ActionType
                    getFlowExportName (action)
                    name
                    getPptHwDevDataTypeText action
                    action.InAddress
                    action.OutAddress
                    action.TaskDevParamIO.InParam.Symbol 
                    action.TaskDevParamIO.OutParam.Symbol 
                ]
            )

        if operatorRows.any() || commandRows.any()  || variRows.any()  || condiRows.any() || actionRows.any()
        then
            let dts =
                condiRows
                @ actionRows
                @ commandRows
                @ operatorRows
                @ variRows
                |> Seq.chunkBySize(IOchunkBySize)
                |> Seq.map(fun rows->
                    let dt = new System.Data.DataTable($"{sys.Name} 외부신호 IO LIST")
                    addIOColumn dt
                    addRows rows dt
                    dt
                )

            dts
        else
            []

    let getErrorRows(sys:DsSystem) =

        let mutable no = 0
        let rowItems (name:string, address :string) =
            no <- no+1
            [
              $"Alarm{no}"
              name
              address
               ]

        let rows =
            let calls = sys.GetAlarmCalls()

            seq {
                //1. call, real 부터
                for call in calls |> Seq.sortBy (fun c -> c.Name) do
                    yield rowItems ($"{call.Name}_센서쇼트이상", call.ErrorSensorOn.Address)
                    yield rowItems ($"{call.Name}_센서단락이상", call.ErrorSensorOff.Address)
                    yield rowItems ($"{call.Name}_감지시간초과이상", call.ErrorOnTimeOver.Address)
                    yield rowItems ($"{call.Name}_감지시간부족이상", call.ErrorOnTimeUnder.Address)
                    yield rowItems ($"{call.Name}_해지시간초과이상", call.ErrorOffTimeOver.Address)
                    yield rowItems ($"{call.Name}_해지시간부족이상", call.ErrorOffTimeUnder.Address)
                    yield rowItems ($"{call.Name}_반대센서오프이상", call.ErrorInterlock.Address)

                for real in sys.GetRealVertices() |> Seq.sortBy (fun r -> r.Name) do
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
        let dt = new System.Data.DataTable($"AlarmTable")
        dt.Columns.Add($"{ErrorColumn.No}", typeof<string>) |> ignore
        dt.Columns.Add($"{ErrorColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ErrorColumn.ErrorAddress}", typeof<string>) |> ignore

        let rows = getErrorRows(sys)

        addRows rows dt

        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ErrorColumn>)) dt

        emptyLine ()
        dt

    let ToAlarmTable (sys: DsSystem)  : DataTable =

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


        let alarmList = getErrorRows(sys)
        let rows=
            alarmList
            |> Seq.map (fun err ->
                                rowItems (err[ErrorColumn.Name|>int])
                        )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt

        emptyLine ()
        dt




    let getLabelTable(name:string) =
        let dt=  new System.Data.DataTable($"{name}")
        dt.Columns.Add($"{TextColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty1}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty2}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty3}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Color}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Ltalic}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.UnderLine}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.StrikeOut}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Bold}", typeof<string>) |> ignore
        dt

    let rowDeviceItems (dev: string) (hasSafety:bool)=
            [
              dev
              ""
              ""
              ""
              if hasSafety then "8388736" else "0"
              "Off"
              "Off"
              "Off"
              "On"
               ]

    let ToDevicesApiTable (sys: DsSystem)  : DataTable =
        let dt = getLabelTable "액션이름"

        let rows =
            let devs =  sys.GetDevicesForHMI()
            devs.Select(fun (dev, _)->
                let text =
                    if dev.MaunualAddress = TextSkip then
                        ""//"·"
                    else
                        "□"

                rowDeviceItems text false

                )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt

        emptyLine ()
        dt


    let ToFlowNamesTable (sys: DsSystem)  : DataTable =

        let dt = getLabelTable "Flow이름"

        let rows =
            sys.GetFlowsOrderByName()
                .Select(fun flow -> rowDeviceItems flow.Name false)

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt

        emptyLine ()
        dt


    let ToWorkNamesTable (sys: DsSystem)  : DataTable =

        let dt = getLabelTable "Work이름"

        let rows =
                  sys.GetVerticesOfRealOrderByName()
                     .Select(fun r -> rowDeviceItems $"{r.Flow.Name}.{r.Name}" false)

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt

        emptyLine ()
        dt


    let ToDevicesTable (sys: DsSystem)  : DataTable =

        let dt = getLabelTable "디바이스이름"

        let rows =
            let devCallSet =  sys.GetDevicesForHMI()
            devCallSet.Select(fun (dev,call)->

                    let hasSafety = call.SafetyConditions.Count > 0
                    rowDeviceItems (dev.FullName) hasSafety)

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt

        emptyLine ()
        dt



    let ToAutoWorkTable (sys: DsSystem) target: DataTable =
        let dt = new System.Data.DataTable("Work자동조작")
        dt.Columns.Add($"{AutoColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.Address}", typeof<string>) |> ignore


        let rowItems (name: string, addr:string) =
            [ name;"bit";addr ]

        let rows =
            sys.GetVerticesOfRealOrderByName()
            |> Seq.collect (fun real ->
                let name = $"{real.Flow.Name}_{real.Name}"
                [
                    yield rowItems ($"{name}_SET", real.V.ON.Address)
                    yield rowItems ($"{name}_RESET", real.V.RF.Address)
                    yield rowItems ($"{name}_START", real.V.SF.Address)
                    yield rowItems ($"{name}_ORIGIN", real.V.OB.Address)
                    yield rowItems ($"{name}_ERROR", real.V.ErrTRX.Address)
                    yield rowItems ($"{name}_STATE_R", real.V.R.Address)
                    yield rowItems ($"{name}_STATE_G", real.V.G.Address)
                    yield rowItems ($"{name}_STATE_F", real.V.F.Address)
                    yield rowItems ($"{name}_STATE_H", real.V.H.Address)
                ]
            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt

        emptyLine ()
        dt


    let ToAutoFlowTable (sys: DsSystem)  target: DataTable =
        let dt = new System.Data.DataTable("Flow자동조작")
        dt.Columns.Add($"{AutoColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.Address}", typeof<string>) |> ignore

        let rowItems (name: string, addr:string) =
            [ name;"bit";addr ]

        let rows =
            sys.GetFlowsOrderByName()
            |> Seq.collect (fun flow ->
                [
                    yield rowItems ($"{flow.Name}_FlowAutoSelect", flow.auto_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowAutoLamp", flow.aop.Address)
                    yield rowItems ($"{flow.Name}_FlowManualSelect", flow.manual_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowManualLamp", flow.mop.Address)
                    yield rowItems ($"{flow.Name}_FlowDriveBtn", flow.drive_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowDriveLamp", flow.d_st.Address)
                    yield rowItems ($"{flow.Name}_FlowPauseBtn", flow.pause_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowPauseLamp", flow.p_st.Address)
                ]
            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt

        emptyLine ()
        dt


    let ToManualTable (sys: DsSystem) (iomType:IOType) : DataTable =
        let tableName =
            match iomType with
            | IOType.Memory  -> "액션(M)"
            | IOType.In      -> "센서(I)"
            | IOType.Out     -> "출력(Q)"
            | _ -> failwith "Invalid action tag"

        let dt = new System.Data.DataTable(tableName)
        dt.Columns.Add($"{ManualColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn.Address}", typeof<string>) |> ignore

        let rowItems (dev: TaskDev, addr:string) =
            [
              dev.FullName
              dev.InDataType.ToPLCText()
              addr
               ]

        let rows =

            let devs = sys.GetDevicesForHMI()
            devs
            |> Seq.collect (fun (dev,_) ->
                 [

                    match iomType with
                    | IOType.Memory ->
                        yield rowItems (dev, if dev.IsMaunualAddressSkipOrEmpty then HMITempManualAction else dev.MaunualAddress)
                    | IOType.In->
                        yield rowItems (dev, if dev.IsInAddressSkipOrEmpty then HMITempMemory else dev.InAddress)
                    | IOType.Out ->
                        yield rowItems (dev, if dev.IsOutAddressSkipOrEmpty then HMITempMemory else dev.OutAddress)

                    | _ -> failwith "Invalid action tag"
                 ]
            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt


        emptyLine ()
        dt



    let ToManualTable_BtnLamp (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"조작반(M)")
        dt.Columns.Add($"{ManualColumn_ControlPanel.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.Manual}", typeof<string>) |> ignore

        let hws = sys.HwSystemDefs
                    .Where(fun f->f :? ButtonDef || f :? LampDef )
                    .Select(fun f-> f.Name, if f :? ButtonDef  then f.InAddress else f.OutAddress)
                    |>dict

        //HMI TAG와 맞춰야 해서 순서  중요
        addRows [[ "AutoSelect"; "bool"; hws["AutoSelect"] ]] dt
        addRows [[ "ManualSelect"; "bool"; hws["ManualSelect"] ]] dt
        addRows [[ "DrivePushBtn"; "bool"; hws["DrivePushBtn"] ]] dt
        addRows [[ "PausePushBtn"; "bool"; hws["PausePushBtn"] ]] dt
        addRows [[ "ClearPushBtn"; "bool"; hws["ClearPushBtn"] ]] dt
        addRows [[ "EmergencyBtn"; "bool"; hws["EmergencyBtn"] ]] dt
        addRows [[ "AutoModeLamp"; "bool"; hws["AutoModeLamp"] ]] dt
        addRows [[ "ManualModeLamp"; "bool"; hws["ManualModeLamp"] ]] dt
        addRows [[ "IdleModeLamp"; "bool"; hws["IdleModeLamp"] ]] dt
        addRows [[ "ErrorLamp"; "bool"; hws["ErrorLamp"] ]] dt
        addRows [[ "OriginStateLamp"; "bool"; hws["OriginStateLamp"] ]] dt
        addRows [[ "ReadyStateLamp"; "bool"; hws["ReadyStateLamp"] ]] dt
        addRows [[ "DriveLamp"; "bool"; hws["DriveLamp"] ]] dt
        addRows [[ "SimulationLamp"; "bool"; RuntimeDS.EmulationAddress ]] dt

        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt

        emptyLine ()
        dt


    let ToIOListDataTables (system: DsSystem) (rowSize:int) (target:HwTarget) =
        let tableDeviceIOs = ToDeviceIOTables system rowSize target
        let tablePanelIO = ToPanelIOTable system  true target
        let tabletableFuncVariExternal = ToFuncVariTables system  target

        let tables = tableDeviceIOs  @ [tablePanelIO ] @ tabletableFuncVariExternal

        tables




    let toDataTablesToJSON (dataTables: seq<DataTable>) (fileName: string) =
        // Helper function to convert DataTable to a list of dictionaries
        let dataTableToJson (dataTable: DataTable) : Dictionary<string, string> list =
            dataTable.Rows
                |> Seq.cast<DataRow>
                |> Seq.map (fun (row: DataRow) ->
                    dataTable.Columns
                    |> Seq.cast<DataColumn>
                    |> Seq.fold (fun (dict: Dictionary<string, string>) (col: DataColumn) ->
                        let field = row.[col] |> string
                        dict.Add(col.ColumnName, field)
                        dict
                    ) (Dictionary<string, string>())
                )
                |> Seq.toList

        // Convert each DataTable into a named entry with rows as JSON
        let jsonContent =
            dataTables
            |> Seq.mapi (fun i dataTable ->
                let data = dataTableToJson dataTable
                // Create an entry per DataTable with its index as the key
                let tableName = sprintf "Table%d" (i + 1)
                (tableName, data)
            )
            |> dict
            |> JsonConvert.SerializeObject

        // Specify the file path and write the JSON content to a file
        let filePath = Path.Combine(Path.GetTempPath(), fileName + ".json")
        File.WriteAllText(filePath, jsonContent, System.Text.Encoding.UTF8)

        filePath


    [<Extension>]
    type OfficeExcelExt =
        [<Extension>]
        static member ExportIOListToExcel (sys: DsSystem) (filePath: string) target=
            let dataTables =  ToIOListDataTables sys ExcelchunkBySize target
            createSpreadsheet filePath (dataTables) 25.0 true

        //[<Extension>] //convertDataSetToPdf 구현 필요 opensource 찾아야함
        //static member ExportIOListToPDF (system: DsSystem) (filePath: string) target=
        //    let dataTables =  ToIOListDataTables system PDFchunkBySize target
        //    convertDataSetToPdf filePath dataTables

        [<Extension>]
        static member ExportHMITableToExcel (sys: DsSystem) (filePath: string) target=
            let dataTables = [|

                ToManualTable sys IOType.Memory
                ToManualTable sys IOType.In
                ToManualTable sys IOType.Out
                ToManualTable_BtnLamp sys

                ToAutoFlowTable sys target
                ToAutoWorkTable sys target

                ToFlowNamesTable sys
                ToWorkNamesTable sys
                ToDevicesTable sys
                ToDevicesApiTable sys
                ToAlarmTable sys

                                |]
            createSpreadsheet filePath dataTables 25.0 false

        [<Extension>]
        static member ToDataJsonFlows  (system: DsSystem) (flowNames:string seq) (conatinSys:bool) target =
            let dataTables = ToIOListDataTables system IOchunkBySize target
            toDataTablesToJSON dataTables "IOTABLE"

        [<Extension>]
        static member ToDataJsonLayouts (xs: Flow seq) =
            let dataTable = ToLayoutTable xs
            toDataTablesToJSON [dataTable] "LAYOUT"
