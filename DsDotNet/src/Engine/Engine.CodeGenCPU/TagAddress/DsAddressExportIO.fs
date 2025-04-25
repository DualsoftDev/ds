namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open Engine.Core
open System.Linq
open Engine.Core.MapperDataModule
open System

[<AutoOpen>]
module DsAddressExportIO =


    let toSymbol opt = if String.IsNullOrWhiteSpace(opt) then "" else opt
    let toAddr addr = if addr =  ""  then  TextAddrEmpty  else addr
    
    let formatDataTypeOpt (inType: DataType ) (outType: DataType ) =
            if inType = outType then
                inType.ToPLCText()
            else
                $"{inType.ToPLCText()}:{outType.ToPLCText()}"


    let ExportDeviceTags (sys: DsSystem) : DeviceTag seq =

        let makeDeviceTag case  flow (name:string) dataType input output symbolIn symbolOut =
            DeviceTag(
                Case = case,
                Flow = flow,
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
                    let dt =  formatDataTypeOpt dev.InDataType dev.OutDataType
                    let flow = 
                        dev.FullName.Contains(DsText.TextDeviceSplit)
                        |> function
                        | true -> dev.FullName.Split(DsText.TextDeviceSplit)[0]
                        | false -> TextAllFlow

                    makeDeviceTag
                        TextTagIOAddress
                        flow
                        dev.FullNameWithoutFlow
                        dt
                        (toAddr dev.InAddress)
                        (toAddr dev.OutAddress)
                        (toSymbol dev.InSymbol)
                        (toSymbol dev.OutSymbol)
                ))

        let exportButtons =
            sys.HWButtons
            |> Seq.map (fun b ->
                let dt =  formatDataTypeOpt b.InDataType b.OutDataType
                let tagCase =
                    match b.ButtonType with
                    | BtnType.DuAutoBTN     -> TextTagIOAutoBTN
                    | BtnType.DuManualBTN   -> TextTagIOManualBTN
                    | BtnType.DuDriveBTN    -> TextTagIODriveBTN
                    | BtnType.DuPauseBTN    -> TextTagIOPauseBTN
                    | BtnType.DuEmergencyBTN-> TextTagIOEmergencyBTN
                    | BtnType.DuTestBTN     -> TextTagIOTestBTN
                    | BtnType.DuReadyBTN    -> TextTagIOReadyBTN
                    | BtnType.DuClearBTN    -> TextTagIOClearBTN
                    | BtnType.DuHomeBTN     -> TextTagIOHomeBTN
                makeDeviceTag tagCase TextAllFlow b.Name dt (toAddr b.InAddress) (toAddr b.OutAddress) (toSymbol b.TaskDevParamIO.InParam.Symbol) (toSymbol b.TaskDevParamIO.OutParam.Symbol)
            )

        let exportLamps =
            sys.HWLamps
            |> Seq.map (fun l ->
                let dt =  formatDataTypeOpt l.InDataType l.OutDataType
                let tagCase =
                    match l.LampType with
                    | LampType.DuAutoModeLamp        -> TextTagIOAutoLamp
                    | LampType.DuManualModeLamp      -> TextTagIOManualLamp
                    | LampType.DuDriveStateLamp      -> TextTagIODriveLamp
                    | LampType.DuErrorStateLamp      -> TextTagIOErrorLamp
                    | LampType.DuTestDriveStateLamp  -> TextTagIOTestLamp
                    | LampType.DuReadyStateLamp      -> TextTagIOReadyLamp
                    | LampType.DuIdleModeLamp        -> TextTagIOIdleLamp
                    | LampType.DuOriginStateLamp     -> TextTagIOHomingLamp
                makeDeviceTag tagCase TextAllFlow l.Name dt (toAddr l.InAddress) (toAddr l.OutAddress) (toSymbol l.TaskDevParamIO.InParam.Symbol) (toSymbol l.TaskDevParamIO.OutParam.Symbol)
            )

        let exportConditions =
            sys.HWConditions
            |> Seq.map (fun c ->
                let dt =  formatDataTypeOpt c.InDataType c.OutDataType
                let tagCase =
                    match c.ConditionType with
                    | ConditionType.DuReadyState -> TextTagIOConditionReady
                    | ConditionType.DuDriveState -> TextTagIOConditionDrive
                makeDeviceTag tagCase TextAllFlow c.Name dt (toAddr c.InAddress) (toAddr c.OutAddress) (toSymbol c.TaskDevParamIO.InParam.Symbol) (toSymbol c.TaskDevParamIO.OutParam.Symbol)
            )

        let exportActions =
            sys.HWActions
            |> Seq.map (fun a ->
                let dt =  formatDataTypeOpt a.InDataType a.OutDataType
                let tagCase =
                    match a.ActionType with
                    | ActionType.DuEmergencyAction -> TextTagIOActionEmg
                    | ActionType.DuPauseAction     -> TextTagIOActionPause
                makeDeviceTag tagCase TextAllFlow a.Name dt (toAddr a.InAddress) (toAddr a.OutAddress) (toSymbol a.TaskDevParamIO.InParam.Symbol) (toSymbol a.TaskDevParamIO.OutParam.Symbol)
            )

        let exportVariables =
            sys.Variables
            |> Seq.map (fun v ->
                let tagCase = 
                    match v.VariableType with
                    | VariableType.ConstType ->    TextTagIOConst
                    | VariableType.VariableType -> TextTagIOVariable

                let input =
                    match v.VariableType with
                    | VariableType.ConstType -> v.InitValue
                    | VariableType.VariableType -> TextNotUsed

                let dataTypeText = v.Type.ToPLCText()
                makeDeviceTag tagCase TextAllFlow v.Name dataTypeText input TextNotUsed "" ""
            )


        let exportFunctions =
            sys.Functions
            |> Seq.map (fun f ->
                match f with
                | :? OperatorFunction as op ->
                    makeDeviceTag "Operator" TextAllFlow  op.Name "" (op.OperatorCode.TrimEnd(';')) "-" "" ""
                | :? CommandFunction as cmd ->
                    makeDeviceTag "Command" TextAllFlow  cmd.Name "" "-" (cmd.CommandCode.TrimEnd(';')) "" ""
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

       