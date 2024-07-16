// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic
open Engine.CodeGenCPU
open Engine.Parser.FS

[<AutoOpen>]
module ImportIOTable =


    let ApplyIO (sys: DsSystem, dts: (int * Data.DataTable) seq) =

        try

            let handleFunctionCreationOrUpdate (sys:DsSystem) funcName funcBodyText isCommand =
                match sys.Functions.TryFind(fun f -> f.Name = funcName) with
                | Some func ->
                    match func with
                    | :? OperatorFunction as op when op.OperatorType = DuOPUnDefined ->
                            updateOperator op funcBodyText
                            Some (op :> Func)

                    | :? CommandFunction as cmd when cmd.CommandType = DuCMDUnDefined ->
                        if funcBodyText = ""
                        then 
                            failWithLog $"error {funcName} function body is empty"
                        else 
                            cmd.CommandType <- DuCMDCode
                            cmd.CommandCode <- funcBodyText
                            Some (cmd :> Func)

                    | _ -> Some func

                | None ->
                    if isCommand 
                    then
                        let func = CommandFunction.Create(funcName, funcBodyText)
                        sys.Functions.Add func |> ignore
                        Some (func)
                    else 
                        let func =  OperatorFunction.Create(funcName, funcBodyText)
                        sys.Functions.Add func |> ignore
                        Some (func)
                       
                            
            
            let getFunctionNUpdate (callName:string, funcName:string, funcBodyText:string, isCommand: bool, page) =
                if ((trimSpace funcBodyText) = "" || funcBodyText = TextSkip || funcBodyText = TextFuncNotUsed)
                then
                    None
                else 
                    let funcBodyText =
                        if funcBodyText.EndsWith(";") 
                        then funcBodyText
                        else $"{funcBodyText};"

                    if isCommand
                    then 
                        handleFunctionCreationOrUpdate sys funcName funcBodyText true
                    else 
                        //let funcName =
                        //    match funcName = "" with
                        //    | true -> $"OP_{funcBodyText}"
                        //    | false -> funcName

                        handleFunctionCreationOrUpdate sys funcName funcBodyText false
                        
                            
            let dicDev =
                sys.Jobs
                |> Seq.collect (fun f -> f.DeviceDefs)
                |> Seq.map (fun j -> j.DeviceApiName, j)
                |> dict

            let dicJob =
                sys.Jobs
                |> Seq.map (fun j -> j.DeviceDefs , j)
                |> Seq.collect (fun (devs, j) -> devs|>Seq.map(fun dev-> dev.ApiName, j))
                |> dict

 
            let getInOutDataType (inOutText:string) =
                if inOutText = ""
                then DuBOOL, DuBOOL
                else
                    let checkInType, checkOutType =
                        if inOutText.Contains(":")
                        then 
                            inOutText.Split(':').Head(), inOutText.Split(':').Last()
                        else
                            inOutText, inOutText 

                    textToDataType checkInType, textToDataType checkOutType
                

            let checkHardwareDataType (name:string) (dataTypeText:string)  (addrIn:string, valueIn:obj) (addrOut:string, valueOut:obj) =
                let checkHardwareDataType (duDataType:DataType, value:obj) =
                    if value.IsNonNull() then
                        let valType =getDataType(value.GetType()) 
                        if valType <> duDataType 
                            then failWithLog $"error datatype : {name}\r\n [{duDataType.ToText()}]  <> value {value}[{valType.ToType().ToDsDataTypeString()}]"
                    else 
                        if duDataType <> DuBOOL 
                            then failWithLog $"Bit 타입이 아니면 func에 값을 입력해야 합니다. \r\n에러 항목 : {name}({dataTypeText})"


                let checkInType, checkOutType = getInOutDataType dataTypeText

                if addrIn  <> TextSkip then checkHardwareDataType (checkInType, valueIn)    
                if addrOut <> TextSkip then checkHardwareDataType (checkOutType, valueOut)    
                
            let getSymbol x = if x <> "" then Some x else None

            let extractHardwareData (row: Data.DataRow) =
                let name = $"{row.[(int) IOColumn.Name]}".Trim()
                let flow = $"{row.[(int) IOColumn.Flow]}".Trim()
                let inOutDataType = $"{row.[(int) IOColumn.DataType]}".Trim()
                let inSymbol = $"{row.[(int) IOColumn.InSymbol]}".Trim()
                let outSymbol = $"{row.[(int) IOColumn.OutSymbol]}".Trim()
                let inAddress = $"{row.[(int) IOColumn.Input]}".Trim()
                let outAddress = $"{row.[(int) IOColumn.Output]}".Trim()
                let name =
                    if $"{TextXlsAllFlow}" = flow
                    then name
        
                    else $"{flow}.{name}"  

                (name, inOutDataType, getSymbol inSymbol, getSymbol outSymbol, inAddress, outAddress)

            let updateDev (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let devName = getDevName row
                let name, dataType, inSym, outSym, inAddress, outAddress = extractHardwareData row
                if not <| dicDev.ContainsKey(devName) then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1006, $"{devName}", page, 0u)
                
                let dev = dicDev.[devName]
                let inAdd =    inAddress|>emptyToSkipAddress
                let outAdd =   outAddress|>emptyToSkipAddress
                let checkInType, checkOutType = getInOutDataType dataType

                dev.InAddress <-  getValidAddress(inAdd, checkInType,   dev.QualifiedName, false, IOType.In,  Util.runtimeTarget)
                dev.OutAddress <-  getValidAddress(outAdd,checkOutType,  dev.QualifiedName, false, IOType.Out, Util.runtimeTarget)


                updatePPTDevParam dev  (inSym,checkInType) (outSym, checkOutType)
             
            let updateVarNConst (row: Data.DataRow, tableIO: Data.DataTable, page, isConst:bool) =
                let name = $"{row.[(int) IOColumn.Name]}"

                if name <> ""
                then
                    let dataType = ($"{row.[(int) IOColumn.DataType]}").Trim() 
                    if dataType = TextSkip || dataType = ""
                    then
                        failwithlog $"변수 {name} datatype 입력이 필요합니다."

                    let value  = ($"{row.[(int) IOColumn.Input]}").Trim()
                    let constVari = value <> "" && value <> TextSkip
                    
                    if isConst && isConst <> constVari
                    then
                        failwithlog $"상수 {name} Input 영역에 상수 값 입력이 필요합니다."

                    let variableData = VariableData(name, dataType|> textToDataType, if constVari then Immutable else Mutable )

                    if constVari
                    then 
                        variableData.InitValue <- value

                    sys.AddVariables(variableData)

            let updateCommand (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = getDevName row
                let func = $"{row.[(int) IOColumn.Output]}"
                if func = "" then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1010, $"{func}", page, 0u)

                if not(func.Contains(" = "))
                        then 
                            failWithLog $"명령 함수는 할당( = ) 구문이 있어야 합니다. {name} {func}"
                        
                getFunctionNUpdate (name, name, func,  true, page) |> ignore

            let updateOperator (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = getDevName row
                let func = $"{row.[(int) IOColumn.Input]}"
                if func = "" then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1010, $"{func}", page, 0u)

                getFunctionNUpdate (name, name, func,   false,  page) |> ignore


            let updateBtn (row: Data.DataRow, btntype: BtnType, tableIO: Data.DataTable, page) =
                let name, dataType, inSym, outSym, inAddress, outAddress = extractHardwareData row
               

                match sys.HWButtons.Where(fun w -> w.ButtonType = btntype).TryFind(fun f -> f.Name = name.DeQuoteOnDemand()) with
                | Some btn ->
                    updateHwAddress (btn) (inAddress, outAddress) Util.runtimeTarget
                    let checkInType, checkOutType = getInOutDataType dataType
                    updatePPTHwParam btn (inSym,checkInType) (outSym, checkOutType)
             
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1001, $"{name}", page, 0u)


            let updateLamp (row: Data.DataRow, lampType: LampType, tableIO: Data.DataTable, page) =
                let name, dataType, inSym, outSym, inAddress, outAddress = extractHardwareData row
                let lamps = sys.HWLamps.Where(fun w -> w.LampType = lampType)

                match lamps.TryFind(fun f -> f.Name = name.DeQuoteOnDemand()) with
                | Some lamp ->
                    updateHwAddress (lamp) (inAddress, outAddress) Util.runtimeTarget
                    let checkInType, checkOutType = getInOutDataType dataType
                    updatePPTHwParam lamp (inSym,checkInType) (outSym, checkOutType)
             

                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1002, $"{name}", page, 0u)

            let updateCondition (row: Data.DataRow, cType: ConditionType, tableIO: Data.DataTable, page) =
                let name, dataType, inSym, outSym, inAddress, outAddress = extractHardwareData row
                let conds = sys.HWConditions.Where(fun w -> w.ConditionType = cType)

                match conds.TryFind(fun f -> f.Name = name.DeQuoteOnDemand()) with
                | Some cond ->
                    updateHwAddress (cond) (inAddress, outAddress) Util.runtimeTarget
                    let checkInType, checkOutType = getInOutDataType dataType
                    updatePPTHwParam cond (inSym,checkInType) (outSym, checkOutType)
             

                   
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1007, $"{name}", page, 0u)

            dts
            |> Seq.iter (fun (page, dt) ->
                let tableIO = dt
              
                for row in tableIO.Rows do
                    let case = $"{row.[(int) IOColumn.Case]}"

                    if
                        (trimSpace (getDevName row) <> "" //name 존재시만
                         && case <> $"{IOColumn.Case}") //header 스킵
                    then
                        match TextToXlsType(case) with
                        | XlsAddress -> updateDev (row, tableIO, page)
                        | XlsVariable -> updateVarNConst (row, tableIO, page, false)
                        | XlsConst ->    updateVarNConst (row, tableIO, page, true)

                        | XlsAutoBTN -> updateBtn (row, BtnType.DuAutoBTN, tableIO, page)
                        | XlsManualBTN -> updateBtn (row, BtnType.DuManualBTN, tableIO, page)
                        | XlsDriveBTN -> updateBtn (row, BtnType.DuDriveBTN, tableIO, page)
                        | XlsPauseBTN -> updateBtn (row, BtnType.DuPauseBTN, tableIO, page)
                        | XlsEmergencyBTN -> updateBtn (row, BtnType.DuEmergencyBTN, tableIO, page)
                        | XlsTestBTN -> updateBtn (row, BtnType.DuTestBTN, tableIO, page)
                        | XlsReadyBTN -> updateBtn (row, BtnType.DuReadyBTN, tableIO, page)
                        | XlsClearBTN -> updateBtn (row, BtnType.DuClearBTN, tableIO, page)
                        | XlsHomeBTN -> updateBtn (row, BtnType.DuHomeBTN, tableIO, page)

                        | XlsAutoLamp -> updateLamp (row, LampType.DuAutoModeLamp, tableIO, page)
                        | XlsManualLamp -> updateLamp (row, LampType.DuManualModeLamp, tableIO, page)
                        | XlsDriveLamp -> updateLamp (row, LampType.DuDriveStateLamp, tableIO, page)
                        | XlsErrorLamp -> updateLamp (row, LampType.DuErrorStateLamp, tableIO, page)
                        | XlsTestLamp -> updateLamp (row, LampType.DuTestDriveStateLamp, tableIO, page)
                        | XlsReadyLamp -> updateLamp (row, LampType.DuReadyStateLamp, tableIO, page)
                        | XlsIdleLamp -> updateLamp (row, LampType.DuIdleModeLamp, tableIO, page)
                        | XlsHomingLamp -> updateLamp (row, LampType.DuOriginStateLamp, tableIO, page)
                        | XlsCommand -> updateCommand (row, tableIO, page) 
                        | XlsOperator -> updateOperator (row, tableIO, page) 

                        | XlsConditionReady -> updateCondition (row, ConditionType.DuReadyState, tableIO, page)

            )

        with ex ->
            failwithf $"{ex.Message}"
