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
open Engine.Core.MapperDataModule
open Engine.CodeGenCPU.DsAddressUtil

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

    let addIOColumn(dt:DataTable) =

        dt.Columns.Add($"{IOColumn.Case}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Flow}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Input}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Output}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.InSymbol}", typeof<string>)  |> ignore
        dt.Columns.Add($"{IOColumn.OutSymbol}", typeof<string>)  |> ignore

    let emptyLine (dt:DataTable) = emptyRow (Enum.GetNames(typedefof<IOColumn>)) dt

    let getFlowExportName(hw:HwSystemDef)  =
        if hw.IsGlobalSystemHw
            then "ALL"
            else String.Join(";", hw.SettingFlows.Select(fun f->f.Name))

    let ToPanelIOTable(sys: DsSystem) (containSys:bool) target : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name} Panel IO LIST")

        addIOColumn dt

        let toBtnText (btns: ButtonDef seq, xlsCase: TagIOCase) =
            for btn in btns do
                if containSys then
                    updateHwAddress (btn) (btn.InAddress, btn.OutAddress) target
                    let dType = getHwDevDataTypeText btn
                    dt.Rows.Add(xlsCase.ToText(), getFlowExportName(btn), btn.Name, dType,  btn.InAddress, btn.OutAddress ,"", "")
                    |> ignore

        let toLampText (lamps: LampDef seq, xlsCase: TagIOCase) =
            for lamp in lamps do
                if containSys then
                    updateHwAddress (lamp) (lamp.InAddress, lamp.OutAddress) target
                    let dType = getHwDevDataTypeText lamp
                    dt.Rows.Add(xlsCase.ToText(), getFlowExportName(lamp), lamp.Name, dType,  lamp.InAddress, lamp.OutAddress ,"", "")
                    |> ignore


        toBtnText  (sys.AutoHWButtons,     TagIOCase.TagIOAutoBTN)
        toBtnText  (sys.ManualHWButtons,   TagIOCase.TagIOManualBTN)
        toBtnText  (sys.DriveHWButtons,    TagIOCase.TagIODriveBTN)
        toBtnText  (sys.PauseHWButtons,    TagIOCase.TagIOPauseBTN)
        toBtnText  (sys.ClearHWButtons,    TagIOCase.TagIOClearBTN)
        toBtnText  (sys.EmergencyHWButtons,TagIOCase.TagIOEmergencyBTN)
        toBtnText  (sys.HomeHWButtons,     TagIOCase.TagIOHomeBTN)
        toBtnText  (sys.TestHWButtons,     TagIOCase.TagIOTestBTN)
        toBtnText  (sys.ReadyHWButtons,    TagIOCase.TagIOReadyBTN)
        toLampText (sys.AutoHWLamps,       TagIOCase.TagIOAutoLamp)
        toLampText (sys.ManualHWLamps,     TagIOCase.TagIOManualLamp)
        toLampText (sys.IdleHWLamps,       TagIOCase.TagIOIdleLamp)
        toLampText (sys.ErrorHWLamps,      TagIOCase.TagIOErrorLamp)
        toLampText (sys.OriginHWLamps,     TagIOCase.TagIOHomingLamp)
        toLampText (sys.ReadyHWLamps,      TagIOCase.TagIOReadyLamp)
        toLampText (sys.DriveHWLamps,      TagIOCase.TagIODriveLamp)
        toLampText (sys.TestHWLamps,       TagIOCase.TagIOTestLamp)


        dt

    let splitNameForRow(name:string) =
        let items = name.Split(TextDeviceSplit) //gpt에서 생성하면  TextDeviceSplit 규격 안따름
        if items.Length = 1 then
            "", items.[0]
        else
            let head = items[0];
            let tail = name[(head.Length+TextDeviceSplit.Length)..]
            head, tail

    let rowIOItems (devIndex:int, dev: TaskDev, job: Job) target =
        let inSym  =  dev.TaskDevParamIO.InParam.Symbol
        let outSym =  dev.TaskDevParamIO.OutParam.Symbol
        let inSkip, outSkip = FindExtension.GetSkipInfo(devIndex, job)

        let flow, name = splitNameForRow $"{dev.DeviceName}.{dev.ApiItem.PureName}"
        [
            TextTagIOAddress
            flow
            name
            getDevDataTypeText (dev)
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
                let devsJob =  sys.GetTaskDevs()
                for g in devsJob.GroupBy(fun (dev, job) -> job) do
                    let mutable devIndex = 0
                    for (dev, job) in g do
                        devIndex <- devIndex + 1
                        if dev.IsRootOnlyDevice
                        then
                            dev.OutAddress <- (TextNotUsed)
                            if dev.InAddress = TextAddrEmpty
                            then
                                dev.InAddress  <-  getExternalTempMemory (target, extCnt)
                                extCnt <- extCnt+1

                        yield rowIOItems (devIndex, dev, job) target
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
                    let funcText = funcOperatorText func.Statements

                    [
                        TextTagIOOperator
                        TextTagIOAllFlow
                        name
                        TextNotUsed
                        funcText
                        TextNotUsed
                        TextNotUsed
                        TextNotUsed
                    ])

        let commandRows =
            sys.Functions
                .OfType<CommandFunction>()
                .Map(fun func->
                    let _flow, name = splitNameForRow func.Name
                    let funcText =    funcText func.Statements

                    [
                        TextTagIOCommand
                        TextTagIOAllFlow
                        name
                        TextNotUsed
                        TextNotUsed
                        funcText
                        TextNotUsed
                        TextNotUsed
                    ])

        let variRows =
            sys.Variables.Map(fun vari->
                [
                    vari.VariableType = VariableType ?= (TextTagIOVariable, TextTagIOConst)
                    TextTagIOAllFlow
                    vari.Name
                    vari.Type.ToText()
                    vari.VariableType = VariableType ?= (TextNotUsed, vari.InitValue)
                    TextNotUsed
                    TextNotUsed
                    TextNotUsed
                ])



        let condiRows =
            let getTagIOConditionLabel (conditionType:ConditionType) =
                match conditionType with
                | DuReadyState  -> TextTagIOConditionReady
                | DuDriveState  -> TextTagIOConditionDrive


            sys.HWConditions
            |> Seq.sortBy(fun cond -> cond.Name)
            |> Seq.map(fun cond ->
                let _, name = splitNameForRow cond.Name
                updateHwAddress (cond) (cond.InAddress, cond.OutAddress) hwTarget
                [
                    getTagIOConditionLabel cond.ConditionType
                    getFlowExportName (cond)
                    name
                    getHwDevDataTypeText cond
                    cond.InAddress
                    cond.OutAddress
                    cond.TaskDevParamIO.InParam.Symbol
                    cond.TaskDevParamIO.OutParam.Symbol
                ]
            )
        let actionRows =
            let getTagIOActionLabel (actionType:ActionType) =
                match actionType with
                | DuEmergencyAction  -> TextTagIOActionEmg
                | DuPauseAction      -> TextTagIOActionPause

            sys.HWActions
            |> Seq.sortBy(fun action -> action.Name)
            |> Seq.map(fun action ->
                let _, name = splitNameForRow action.Name
                updateHwAddress (action) (action.InAddress, action.OutAddress) hwTarget
                [
                    getTagIOActionLabel action.ActionType
                    getFlowExportName (action)
                    name
                    getHwDevDataTypeText action
                    action.InAddress
                    action.OutAddress
                    action.TaskDevParamIO.InParam.Symbol
                    action.TaskDevParamIO.OutParam.Symbol
                ]
            )

        if operatorRows.Any() || commandRows.Any()  || variRows.Any()  || condiRows.Any() || actionRows.Any()
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
                    yield rowItems ($"{call.Name}_센서단선이상", call.ErrorSensorOff.Address)
                    yield rowItems ($"{call.Name}_감지시간초과이상", call.ErrorOnTimeOver.Address)
                    yield rowItems ($"{call.Name}_감지시간부족이상", call.ErrorOnTimeUnder.Address)
                    yield rowItems ($"{call.Name}_해지시간초과이상", call.ErrorOffTimeOver.Address)
                    yield rowItems ($"{call.Name}_해지시간부족이상", call.ErrorOffTimeUnder.Address)
                    yield rowItems ($"{call.Name}_반대센서이상", call.ErrorInterlock.Address)

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
                rowItems (err[int ErrorColumn.Name]) )

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
                    if dev.ManualAddress = TextNotUsed then
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
                    yield rowItems ($"{name}_ERROR", real.V.ErrWork.Address)
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
                        yield rowItems (dev, if dev.IsManualAddressSkipOrEmpty then HMITempManualAction else dev.ManualAddress)
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

    let simAddress (target:HwTarget)  (pakage:RuntimeMode) =
            let isSim = pakage.IsVirtualMode()
            match target.HwIO with   
            | HwIO.LS_XGK_IO -> 
                if isSim then ExternalXGKAddressON else ExternalXGKAddressOFF
            | HwIO.LS_XGI_IO -> 
                if isSim then ExternalXGIAddressON else ExternalXGIAddressOFF
            |_ -> failwith $"{target.HwIO} Invalid simAddress tag" 

    let ToManualTable_BtnLamp (sys: DsSystem) (target:HwTarget)  (pakage:RuntimeMode) : DataTable =

        let dt = new System.Data.DataTable($"조작반(M)")
        dt.Columns.Add($"{ManualColumn_ControlPanel.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.Manual}", typeof<string>) |> ignore

        let hws = sys.HwSystemDefs
                    .Where(fun f->f :? ButtonDef || f :? LampDef )
                    .Select(fun f-> f.Name, if f :? ButtonDef  then f.InAddress else f.OutAddress)
                    |>dict
        let simAddress = simAddress target pakage   

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
        addRows [[ "SimulationLamp"; "bool"; simAddress]] dt
        
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt

        emptyLine ()
        dt

    let GetDeviceTags (sys: DsSystem) : DeviceTag[] =

        let hws =
            sys.HwSystemDefs
            |> Seq.filter (fun f -> f :? ButtonDef || f :? LampDef)
            |> Seq.map (fun f -> f.Name, if f :? ButtonDef then f.InAddress else f.OutAddress)
            |> dict

        let tag (case:TagIOCase) name dataType inAddr outAddr : DeviceTag =
            let devTag = DeviceTag()
            devTag.Case <- case.ToText()
            devTag.Flow  <- "ALL"
            devTag.Name  <- name
            devTag.DataType  <- dataType
            devTag.Input   <- inAddr 
            devTag.Output  <- outAddr 
            devTag

        [|
            tag  TagIOAutoBTN "AutoSelect" "bool"         hws["AutoSelect"]   TextNotUsed 
            tag  TagIOManualBTN "ManualSelect" "bool"     hws["ManualSelect"] TextNotUsed 
            tag  TagIODriveBTN "DrivePushBtn" "bool"      hws["DrivePushBtn"] TextNotUsed 
            tag  TagIOPauseBTN "PausePushBtn" "bool"      hws["PausePushBtn"] TextNotUsed 
            tag  TagIOClearBTN "ClearPushBtn" "bool"      hws["ClearPushBtn"] TextNotUsed 
            tag  TagIOEmergencyBTN "EmergencyBtn" "bool"  hws["EmergencyBtn"] TextNotUsed
            tag  TagIOAutoLamp "AutoModeLamp" "bool"      TextNotUsed hws["AutoModeLamp"]
            tag  TagIOManualLamp "ManualModeLamp" "bool"  TextNotUsed hws["ManualModeLamp"]
            tag  TagIOIdleLamp "IdleModeLamp" "bool"      TextNotUsed hws["IdleModeLamp"]
            tag  TagIOErrorLamp "ErrorLamp" "bool"        TextNotUsed hws["ErrorLamp"]
            tag  TagIOHomingLamp "OriginStateLamp" "bool" TextNotUsed hws["OriginStateLamp"]
            tag  TagIOReadyLamp "ReadyStateLamp" "bool"   TextNotUsed hws["ReadyStateLamp"]
            tag  TagIODriveLamp "DriveLamp" "bool"        TextNotUsed  hws["DriveLamp"]
        |]

    let ToIOListDataTables (system: DsSystem) (rowSize:int) (target:HwTarget) =
        let tableDeviceIOs = ToDeviceIOTables system rowSize target
        let tablePanelIO = ToPanelIOTable system  true target
        let tabletableFuncVariExternal = ToFuncVariTables system  target

        let tables = tableDeviceIOs @ [tablePanelIO ] @ tabletableFuncVariExternal

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

        [<Extension>]
        static member ExportHMITableToExcel (sys: DsSystem) (filePath: string) (target:HwTarget) (package:RuntimeMode)=
            let dataTables = [|

                ToManualTable sys IOType.Memory
                ToManualTable sys IOType.In
                ToManualTable sys IOType.Out
                ToManualTable_BtnLamp sys target package

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
        static member ToDataJsonLayouts (xs: Flow seq) =
            let dataTable = ToLayoutTable xs
            toDataTablesToJSON [dataTable] "LAYOUT"
