// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open Engine.CodeGenCPU
open Engine.Core.MapperDataModule;

[<AutoOpen>]
module ImportIODevModule =
    let ApplyIO (sys: DsSystem, devTags: DeviceTag seq, hwTarget: HwTarget) =

        let dicDev =
            sys.Jobs
            |> Seq.collect (fun job -> job.TaskDefs.Select(fun td -> td.FullName, td))
            |> dict

        let getInOutDataType (inOutText: string) =
            if inOutText = "" then DuBOOL, DuBOOL
            else
                let parts = inOutText.Split(':')
                let checkInType, checkOutType =
                    if parts.Length = 2 then parts.[0], parts.[1] else inOutText, inOutText
                textToDataType checkInType, textToDataType checkOutType

        let extractHardwareData (tag: DeviceTag) =
            let name = tag.Name.Trim()
            let dataType = tag.DataType.Trim()
            let inSym = if tag.SymbolIn <> "" then Some (tag.SymbolIn.Trim()) else None
            let outSym = if tag.SymbolOut <> "" then Some (tag.SymbolOut.Trim()) else None
            let inAddr = tag.Input.Trim()
            let outAddr = tag.Output.Trim()

            if tag.SymbolIn.Contains(":") || tag.SymbolOut.Contains(":") then
                failWithLog $"Symbol에 ':' 사용 불가능 {name} {tag.SymbolIn} {tag.SymbolOut}"

            name, dataType, inSym, outSym, inAddr, outAddr

        let updateDev (tag: DeviceTag) =
            let devName = tag.Name
            let name, dataType, inSym, outSym, inAddr, outAddr = extractHardwareData tag
            if dicDev.ContainsKey(devName) then
                let dev = dicDev.[devName]
                let inAddrValid = inAddr |> emptyToSkipAddress
                let outAddrValid = outAddr |> emptyToSkipAddress
                let inType, outType = getInOutDataType dataType

                dev.InAddress <- getValidAddress(inAddrValid, inType, dev.QualifiedName, false, IOType.In, hwTarget)
                dev.OutAddress <- getValidAddress(outAddrValid, outType, dev.QualifiedName, false, IOType.Out, hwTarget)

                updatePptTaskDevParam dev (inSym, inType) (outSym, outType)
            else
                logDebug $"모델에 {devName} 이름이 없습니다."

        let updateVarOrConst (tag: DeviceTag) (isConst: bool) =
            let name = tag.Name.Trim()
            let dataType = tag.DataType.Trim()
            let value = tag.Input.Trim()

            if dataType = "" then
                failWithLog $"상수 {name} datatype 입력이 필요합니다."
            elif isConst && (value = "" || value = TextNotUsed) then
                failWithLog $"상수 {name} Input 영역에 상수 값 입력이 필요합니다."
            elif not isConst && value <> TextNotUsed then
                failWithLog $"변수 {name} Input 영역에 값 입력이 없어야 합니다. ('-' 입력)"

            let variableData = VariableData(name, textToDataType dataType, if isConst then Immutable else Mutable)
            if isConst then variableData.InitValue <- value

            sys.AddVariables(variableData)

        let updateFunction (tag: DeviceTag, isCommand: bool) =
            let name = tag.Name
            let funcBody = if isCommand then tag.Output.Trim() else tag.Input.Trim()

            if funcBody = "" then
                failWithLog $"명령 함수 본문이 비어 있습니다. {name}"

            if isCommand && not (funcBody.Contains(" = ")) then
                failWithLog $"명령 함수는 '=' 할당문이 필요합니다. {name} {funcBody}"

            let body = funcBody + (if funcBody.EndsWith(";") then "" else ";")

            match sys.Functions.TryFind(fun f -> f.Name = name) with
            | Some (:? OperatorFunction as op) -> updateOperator op body; ()
            | Some (:? CommandFunction as cmd) -> cmd.CommandCode <- body
            | Some _ -> ()
            | None ->
                let func =
                    if isCommand then CommandFunction.Create(name, body) :> DsFunc
                    else OperatorFunction.Create(name, body) :> DsFunc
                sys.Functions.Add func |> ignore

        let updateHardwareBtn (tag: DeviceTag, btnType: BtnType) =
            let name = tag.Name.Trim().DeQuoteOnDemand()
            sys.HWButtons
            |> Seq.tryFind (fun b -> b.Name = name && b.ButtonType = btnType)
            |> Option.iter (fun b ->
                updateHwAddress b (tag.Input, tag.Output) hwTarget
                let inType, outType = getInOutDataType tag.DataType
                updatePptHwParam b (Some tag.SymbolIn, inType) (Some tag.SymbolOut, outType))

        let updateHardwareLamp (tag: DeviceTag, lampType: LampType) =
            let name = tag.Name.Trim().DeQuoteOnDemand()
            sys.HWLamps
            |> Seq.tryFind (fun l -> l.Name = name && l.LampType = lampType)
            |> Option.iter (fun l ->
                updateHwAddress l (tag.Input, tag.Output) hwTarget
                let inType, outType = getInOutDataType tag.DataType
                updatePptHwParam l (Some tag.SymbolIn, inType) (Some tag.SymbolOut, outType))

        let updateCondition (tag: DeviceTag, condType: ConditionType) =
            let inType, outType = getInOutDataType tag.DataType
            let inSym = if tag.SymbolIn <> "" then Some (tag.SymbolIn.Trim()) else None
            let outSym = if tag.SymbolOut <> "" then Some (tag.SymbolOut.Trim()) else None

            sys.HWConditions
            |> Seq.tryFind (fun c -> c.Name = tag.Name && c.ConditionType = condType)
            |> Option.iter (fun cond ->
                updateHwAddress cond (tag.Input, tag.Output) hwTarget
                updatePptHwParam cond (inSym, inType) (outSym, outType)
            )


        let updateAction (tag: DeviceTag, actType: ActionType) =
            let inType, outType = getInOutDataType tag.DataType
            let inSym = if tag.SymbolIn <> "" then Some (tag.SymbolIn.Trim()) else None
            let outSym = if tag.SymbolOut <> "" then Some (tag.SymbolOut.Trim()) else None

            sys.HWActions
            |> Seq.tryFind (fun a -> a.Name = tag.Name && a.ActionType = actType)
            |> Option.iter (fun act ->
                updateHwAddress act (tag.Input, tag.Output) hwTarget
                updatePptHwParam act (inSym, inType) (outSym, outType)
            )


        for tag in devTags do
            let case = tag.Case.Trim()
            let name = tag.Name.Trim()
            if name <> "" then
                match TextToXlsType(case) with
                | XlsAddress         -> updateDev tag
                | XlsVariable        -> updateVarOrConst tag false
                | XlsConst           -> updateVarOrConst tag true
                | XlsCommand         -> updateFunction(tag, true)
                | XlsOperator        -> updateFunction(tag, false)
                | XlsConditionReady  -> updateCondition (tag, ConditionType.DuReadyState)
                | XlsConditionDrive  -> updateCondition (tag, ConditionType.DuDriveState)
                | XlsActionEmg       -> updateAction (tag, ActionType.DuEmergencyAction)
                | XlsActionPause     -> updateAction (tag, ActionType.DuPauseAction)

                // 버튼
                | XlsAutoBTN         -> updateHardwareBtn (tag, BtnType.DuAutoBTN)
                | XlsManualBTN       -> updateHardwareBtn (tag, BtnType.DuManualBTN)
                | XlsDriveBTN        -> updateHardwareBtn (tag, BtnType.DuDriveBTN)
                | XlsPauseBTN        -> updateHardwareBtn (tag, BtnType.DuPauseBTN)
                | XlsEmergencyBTN    -> updateHardwareBtn (tag, BtnType.DuEmergencyBTN)
                | XlsTestBTN         -> updateHardwareBtn (tag, BtnType.DuTestBTN)
                | XlsReadyBTN        -> updateHardwareBtn (tag, BtnType.DuReadyBTN)
                | XlsClearBTN        -> updateHardwareBtn (tag, BtnType.DuClearBTN)
                | XlsHomeBTN         -> updateHardwareBtn (tag, BtnType.DuHomeBTN)

                // 램프
                | XlsAutoLamp        -> updateHardwareLamp (tag, LampType.DuAutoModeLamp)
                | XlsManualLamp      -> updateHardwareLamp (tag, LampType.DuManualModeLamp)
                | XlsDriveLamp       -> updateHardwareLamp (tag, LampType.DuDriveStateLamp)
                | XlsErrorLamp       -> updateHardwareLamp (tag, LampType.DuErrorStateLamp)
                | XlsTestLamp        -> updateHardwareLamp (tag, LampType.DuTestDriveStateLamp)
                | XlsReadyLamp       -> updateHardwareLamp (tag, LampType.DuReadyStateLamp)
                | XlsIdleLamp        -> updateHardwareLamp (tag, LampType.DuIdleModeLamp)
                | XlsHomingLamp      -> updateHardwareLamp (tag, LampType.DuOriginStateLamp)
