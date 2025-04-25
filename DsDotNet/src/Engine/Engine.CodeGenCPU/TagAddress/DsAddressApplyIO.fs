namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open PLC.CodeGen.Common
open Dual.PLC.Common.FS
open XgtProtocol
open Engine.CodeGenCPU.DsAddressUtil
open Engine.Core
open System.Linq
open Engine.Core.MapperDataModule

[<AutoOpen>]
module DsAddressApplyIO =

    let checkDataType name (taskDevParamDataType:DataType) (dataType:DataType)=
            if taskDevParamDataType <> dataType
                then failWithLog $"error datatype : {name}\r\n [{taskDevParamDataType.ToPLCText()}]  <> {dataType.ToPLCText()}]"

    let updateTaskDevParam (dev:TaskDev) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  =
        if inSym.IsSome then  dev.SetInSymbol(inSym.Value)
        if outSym.IsSome then  dev.SetOutSymbol(outSym.Value)

        checkDataType $"IN {dev.QualifiedName}" dev.InDataType inDataType
        checkDataType $"OUT {dev.QualifiedName}" dev.OutDataType outDataType

    let getDevDataTypeText (dev:TaskDev) =   DsTaskDevTypeModule.getTaskDevDataTypeText dev.TaskDevParamIO
    let getHwDevDataTypeText (hwDev:HwSystemDef) = DsTaskDevTypeModule.getTaskDevDataTypeText hwDev.TaskDevParamIO

    let updateHwParam (hwDev:HwSystemDef) (inSym:string option, inDataType:DataType)  (outSym:string option, outDataType:DataType)  =
        if inSym.IsSome 
        then hwDev.TaskDevParamIO.InParam.Symbol <- inSym.Value
        if outSym.IsSome 
        then hwDev.TaskDevParamIO.InParam.Symbol <- outSym.Value

        checkDataType  $"IN {hwDev.QualifiedName}" hwDev.InDataType inDataType
        checkDataType  $"OUT {hwDev.QualifiedName}" hwDev.OutDataType outDataType

    let ApplyDeviceTags (sys: DsSystem, devTags: DeviceTag seq) =
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
            let name = tag.DeviceApiName
            let dataType = tag.DataType.Trim()
            let inSym = if tag.SymbolIn <> "" then Some (tag.SymbolIn.Trim()) else None
            let outSym = if tag.SymbolOut <> "" then Some (tag.SymbolOut.Trim()) else None
            let inAddr = tag.Input.Trim()
            let outAddr = tag.Output.Trim()

            if tag.SymbolIn.Contains(":") || tag.SymbolOut.Contains(":") then
                failWithLog $"Symbol에 ':' 사용 불가능 {name} {tag.SymbolIn} {tag.SymbolOut}"

            name, dataType, inSym, outSym, inAddr, outAddr

        let updateDev (tag: DeviceTag) =
            let name, dataType, inSym, outSym, inAddr, outAddr = extractHardwareData tag
            if dicDev.ContainsKey(name) then
                let dev = dicDev.[name]
                let inAddrValid = inAddr |> emptyToSkipAddress
                let outAddrValid = outAddr |> emptyToSkipAddress
                let inType, outType = getInOutDataType dataType

                dev.InAddress <-  inAddrValid  
                dev.OutAddress <- outAddrValid 

                updateTaskDevParam dev (inSym, inType) (outSym, outType)
            else
                failwith $"모델에 {name} 이름이 없습니다."

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

            let variableData = VariableData(name, textToDataType dataType, if isConst then ConstType else VariableType)
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
                b.InAddress <-  tag.Input  
                b.OutAddress <-  tag.Output 

                let inType, outType = getInOutDataType tag.DataType
                updateHwParam b (Some tag.SymbolIn, inType) (Some tag.SymbolOut, outType))

        let updateHardwareLamp (tag: DeviceTag, lampType: LampType) =
            let name = tag.Name.Trim().DeQuoteOnDemand()
            sys.HWLamps
            |> Seq.tryFind (fun l -> l.Name = name && l.LampType = lampType)
            |> Option.iter (fun l ->
                l.InAddress <-  tag.Input  
                l.OutAddress <-  tag.Output 
                let inType, outType = getInOutDataType tag.DataType
                updateHwParam l (Some tag.SymbolIn, inType) (Some tag.SymbolOut, outType))

        let updateCondition (tag: DeviceTag, condType: ConditionType) =
            let inType, outType = getInOutDataType tag.DataType
            let inSym = if tag.SymbolIn <> "" then Some (tag.SymbolIn.Trim()) else None
            let outSym = if tag.SymbolOut <> "" then Some (tag.SymbolOut.Trim()) else None

            sys.HWConditions
            |> Seq.tryFind (fun c -> c.Name = tag.Name && c.ConditionType = condType)
            |> Option.iter (fun cond ->
                cond.InAddress <-  tag.Input  
                cond.OutAddress <-  tag.Output 
                updateHwParam cond (inSym, inType) (outSym, outType)
            )


        let updateAction (tag: DeviceTag, actType: ActionType) =
            let inType, outType = getInOutDataType tag.DataType
            let inSym = if tag.SymbolIn <> "" then Some (tag.SymbolIn.Trim()) else None
            let outSym = if tag.SymbolOut <> "" then Some (tag.SymbolOut.Trim()) else None

            sys.HWActions
            |> Seq.tryFind (fun a -> a.Name = tag.Name && a.ActionType = actType)
            |> Option.iter (fun act ->
                act.InAddress <-  tag.Input  
                act.OutAddress <-  tag.Output 
                updateHwParam act (inSym, inType) (outSym, outType)
            )


        for tag in devTags do
            let case = tag.Case.Trim()
            let name = tag.Name.Trim()
            if name <> "" then
                match TextToTagIOType(case) with
                | TagIOAddress         -> updateDev tag
                | TagIOVariable        -> updateVarOrConst tag false
                | TagIOConst           -> updateVarOrConst tag true
                | TagIOCommand         -> updateFunction(tag, true)
                | TagIOOperator        -> updateFunction(tag, false)
                | TagIOConditionReady  -> updateCondition (tag, ConditionType.DuReadyState)
                | TagIOConditionDrive  -> updateCondition (tag, ConditionType.DuDriveState)
                | TagIOActionEmg       -> updateAction (tag, ActionType.DuEmergencyAction)
                | TagIOActionPause     -> updateAction (tag, ActionType.DuPauseAction)

                // 버튼
                | TagIOAutoBTN         -> updateHardwareBtn (tag, BtnType.DuAutoBTN)
                | TagIOManualBTN       -> updateHardwareBtn (tag, BtnType.DuManualBTN)
                | TagIODriveBTN        -> updateHardwareBtn (tag, BtnType.DuDriveBTN)
                | TagIOPauseBTN        -> updateHardwareBtn (tag, BtnType.DuPauseBTN)
                | TagIOEmergencyBTN    -> updateHardwareBtn (tag, BtnType.DuEmergencyBTN)
                | TagIOTestBTN         -> updateHardwareBtn (tag, BtnType.DuTestBTN)
                | TagIOReadyBTN        -> updateHardwareBtn (tag, BtnType.DuReadyBTN)
                | TagIOClearBTN        -> updateHardwareBtn (tag, BtnType.DuClearBTN)
                | TagIOHomeBTN         -> updateHardwareBtn (tag, BtnType.DuHomeBTN)

                // 램프
                | TagIOAutoLamp        -> updateHardwareLamp (tag, LampType.DuAutoModeLamp)
                | TagIOManualLamp      -> updateHardwareLamp (tag, LampType.DuManualModeLamp)
                | TagIODriveLamp       -> updateHardwareLamp (tag, LampType.DuDriveStateLamp)
                | TagIOErrorLamp       -> updateHardwareLamp (tag, LampType.DuErrorStateLamp)
                | TagIOTestLamp        -> updateHardwareLamp (tag, LampType.DuTestDriveStateLamp)
                | TagIOReadyLamp       -> updateHardwareLamp (tag, LampType.DuReadyStateLamp)
                | TagIOIdleLamp        -> updateHardwareLamp (tag, LampType.DuIdleModeLamp)
                | TagIOHomingLamp      -> updateHardwareLamp (tag, LampType.DuOriginStateLamp)

