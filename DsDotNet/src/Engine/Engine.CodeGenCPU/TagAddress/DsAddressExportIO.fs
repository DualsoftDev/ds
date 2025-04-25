namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open PLC.CodeGen.Common
open Dual.PLC.Common.FS
open XgtProtocol
open Engine.CodeGenCPU.DsAddressUtil
open Engine.Core
open System.Linq
open Engine.Core.MapperDataModule
open System

[<AutoOpen>]
module DsAddressExportIO =


    let ExportDeviceTags (sys: DsSystem) : DeviceTag seq =
        
        let toSymbol opt = if String.IsNullOrWhiteSpace(opt) then "" else opt

        let toAddr addr = if addr = TextAddrEmpty || addr = TextNotUsed then "" else addr

        let makeDeviceTag case name dataType input output symbolIn symbolOut =
            DeviceTag(
                Case = case,
                Name = name,
                DataType = dataType,
                Input = input,
                Output = output,
                SymbolIn = symbolIn,
                SymbolOut = symbolOut
            )

        let exportJobDevices =
            sys.Jobs
            |> Seq.collect (fun job ->
                job.TaskDefs
                |> Seq.map (fun dev ->
                    let dt = $"{dev.InDataType.ToPLCText()}:{dev.OutDataType.ToPLCText()}"
                    makeDeviceTag
                        "Address"
                        dev.FullName
                        dt
                        (toAddr dev.InAddress)
                        (toAddr dev.OutAddress)
                        (toSymbol dev.InSymbol)
                        (toSymbol dev.OutSymbol)
                ))

        let exportButtons =
            sys.HWButtons
            |> Seq.map (fun b ->
                let dt = $"{b.InDataType.ToPLCText()}:{b.OutDataType.ToPLCText()}"
                let tagCase =
                    match b.ButtonType with
                    | BtnType.DuAutoBTN     -> "AutoBTN"
                    | BtnType.DuManualBTN   -> "ManualBTN"
                    | BtnType.DuDriveBTN    -> "DriveBTN"
                    | BtnType.DuPauseBTN    -> "PauseBTN"
                    | BtnType.DuEmergencyBTN-> "EmergencyBTN"
                    | BtnType.DuTestBTN     -> "TestBTN"
                    | BtnType.DuReadyBTN    -> "ReadyBTN"
                    | BtnType.DuClearBTN    -> "ClearBTN"
                    | BtnType.DuHomeBTN     -> "HomeBTN"
                makeDeviceTag tagCase b.Name dt (toAddr b.InAddress) (toAddr b.OutAddress) (toSymbol b.TaskDevParamIO.InParam.Symbol) (toSymbol b.TaskDevParamIO.OutParam.Symbol)
            )

        let exportLamps =
            sys.HWLamps
            |> Seq.map (fun l ->
                let dt = $"{l.InDataType.ToPLCText()}:{l.OutDataType.ToPLCText()}"
                let tagCase =
                    match l.LampType with
                    | LampType.DuAutoModeLamp    -> "AutoLamp"
                    | LampType.DuManualModeLamp  -> "ManualLamp"
                    | LampType.DuDriveStateLamp  -> "DriveLamp"
                    | LampType.DuErrorStateLamp  -> "ErrorLamp"
                    | LampType.DuTestDriveStateLamp -> "TestLamp"
                    | LampType.DuReadyStateLamp  -> "ReadyLamp"
                    | LampType.DuIdleModeLamp    -> "IdleLamp"
                    | LampType.DuOriginStateLamp -> "HomingLamp"
                makeDeviceTag tagCase l.Name dt (toAddr l.InAddress) (toAddr l.OutAddress) (toSymbol l.TaskDevParamIO.InParam.Symbol) (toSymbol l.TaskDevParamIO.OutParam.Symbol)
            )

        let exportConditions =
            sys.HWConditions
            |> Seq.map (fun c ->
                let dt = $"{c.InDataType.ToPLCText()}:{c.OutDataType.ToPLCText()}"
                let tagCase =
                    match c.ConditionType with
                    | ConditionType.DuReadyState -> "ConditionReady"
                    | ConditionType.DuDriveState -> "ConditionDrive"
                makeDeviceTag tagCase c.Name dt (toAddr c.InAddress) (toAddr c.OutAddress) (toSymbol c.TaskDevParamIO.InParam.Symbol) (toSymbol c.TaskDevParamIO.OutParam.Symbol)
            )

        let exportActions =
            sys.HWActions
            |> Seq.map (fun a ->
                let dt = $"{a.InDataType.ToPLCText()}:{a.OutDataType.ToPLCText()}"
                let tagCase =
                    match a.ActionType with
                    | ActionType.DuEmergencyAction -> "ActionEmg"
                    | ActionType.DuPauseAction     -> "ActionPause"
                makeDeviceTag tagCase a.Name dt (toAddr a.InAddress) (toAddr a.OutAddress) (toSymbol a.TaskDevParamIO.InParam.Symbol) (toSymbol a.TaskDevParamIO.OutParam.Symbol)
            )

        let exportVariables =
            sys.Variables
            |> Seq.map (fun v ->
                let tagCase = 
                    match v.VariableType with
                    | VariableType.ConstType -> "Const"
                    | VariableType.VariableType -> "Variable"

                let input =
                    match v.VariableType with
                    | VariableType.ConstType -> v.InitValue
                    | VariableType.VariableType -> TextNotUsed

                let dataTypeText = v.Type.ToPLCText()
                makeDeviceTag tagCase v.Name dataTypeText input TextNotUsed "" ""
            )


        let exportFunctions =
            sys.Functions
            |> Seq.map (fun f ->
                match f with
                | :? OperatorFunction as op ->
                    makeDeviceTag "Operator" op.Name "" (op.OperatorCode.TrimEnd(';')) "-" "" ""
                | :? CommandFunction as cmd ->
                    makeDeviceTag "Command" cmd.Name "" "-" (cmd.CommandCode.TrimEnd(';')) "" ""
                | _ -> null
            )
            |> Seq.filter (fun x -> x <> null)

        Seq.concat [
            exportJobDevices
            exportButtons
            exportLamps
            exportConditions
            exportActions
            exportVariables
            exportFunctions
        ]

       