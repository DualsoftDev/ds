// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic
open Engine.CodeGenCPU

[<AutoOpen>]
module ImportIOTable =

    [<Flags>]
    type IOColumn =
        | Case = 0
        | Name = 1
        | DataType = 2
        | Input = 3
        | Output = 4
        | Func = 5

    [<Flags>]
    type ErrorColumn =
        | No = 0
        | Name = 1
        | ErrorAddress = 2

    [<Flags>]
    type TextColumn =
        | Name = 0
        | Empty1 = 1
        | Empty2 = 2
        | Empty3 = 3
        | Color = 4
        | Ltalic = 5
        | UnderLine = 6
        | StrikeOut = 7
        | Bold = 8

    [<Flags>]
    type ManualColumn_I =
        | Name = 0
        | DataType = 1
        | Input = 2

    [<Flags>]
    type ManualColumn_O =
        | Name = 0
        | DataType = 1
        | Output = 2   

    [<Flags>]
    type ManualColumn_M =
        | Name = 0
        | DataType = 1
        | Manual = 2

    [<Flags>]
    type ManualColumn_ControlPanel =
        | Name = 0
        | DataType = 1
        | Manual = 2

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
                       
            let getAutoGenFuncName funcBodyText =
                let opType, args = getOperatorTypeNArgs(funcBodyText)
                let paras =
                    if args.any()
                    then 
                        [ for param in args do
                                $"_{param}";
                        ] |> String.concat "" 
                    else ""
                $"{opType.ToText().TrimStart('$')}{paras}" 
                            
            
            let getFunctionNUpdate (callName:string, funcName:string, funcBodyText:string, isCommand: bool, page) =
                if ((trimSpace funcBodyText) = "" || funcBodyText = TextSkip || funcBodyText = TextFuncNotUsed)
                then
                    None
                else 
                    //let funcTypeText, parms = 
                    //    match tryGetOperatorType funcBodyText with
                    //    | Some _ -> 
                    //        if funcBodyText.Contains(';') 
                    //        then 
                    //            Office.ErrorPPT(ErrorCase.Name, ErrID._1008, $"{callName}", page, 0u)
                            
                    //        getOperatorTypeNArgs (funcBodyText)
                    //    | None ->  funcDefine, [|funcBodyText|]

                    //if (not <| isJob) && funcTypeText <> "$n" then
                    //    Office.ErrorPPT(ErrorCase.Name, ErrID._1005, $"{funcTypeText}", page, 0u)

                    if isCommand
                    then 
                        handleFunctionCreationOrUpdate sys funcName funcBodyText true
                    else 
                        let funcName =
                            match funcName = "" with
                            | true -> getAutoGenFuncName funcBodyText
                            | false -> funcName

                        handleFunctionCreationOrUpdate sys funcName funcBodyText false
                        
                            
            let dicDev =
                sys.Jobs
                |> Seq.collect (fun f -> f.DeviceDefs)
                |> Seq.map (fun j -> j.ApiName, j)
                |> dict

            let dicJob =
                sys.Jobs
                |> Seq.map (fun j -> j.DeviceDefs , j)
                |> Seq.collect (fun (devs, j) -> devs|>Seq.map(fun dev-> dev.ApiName, j))
                |> dict

            let getOperatorFuntion (f:Func) =
                if  not(f :? OperatorFunction)
                    then
                        failWithLog $"error {f.Name} is not OperatorFunction"
                    else 
                        f :?> OperatorFunction

            let updateDev (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let devName = $"{row.[(int) IOColumn.Name]}"

                if not <| dicDev.ContainsKey(devName) then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1006, $"{devName}", page, 0u)

                let dev = dicDev.[devName]
                let inAdd =    $"{row.[(int) IOColumn.Input]}".Trim() |>emptyToSkipAddress
                let outAdd =   $"{row.[(int) IOColumn.Output]}".Trim()|>emptyToSkipAddress

                dev.InAddress  <-  getValidAddress(inAdd,   dev.QualifiedName, false, IOType.In,  Util.runtimeTarget)
                dev.OutAddress <-  getValidAddress(outAdd,  dev.QualifiedName, false, IOType.Out, Util.runtimeTarget)

                let job = dicJob[devName]
                
                let func = $"{row.[(int) IOColumn.Func]}"
                match getFunctionNUpdate (job.Name, "", func,   false, page) with
                |Some f ->  
                        if  not(f :? OperatorFunction)
                        then
                            failWithLog $"error {f.Name} is not OperatorFunction"
                        let op = f :?> OperatorFunction
                        job.OperatorFunction <- Some op
                        sys.Functions.Add op
                |_->()
                        
             
            let updateVar (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let dataType = $"{row.[(int) IOColumn.DataType]}" |> textToDataType
                let variableData = VariableData(name, dataType)
                sys.Variables.Add(variableData)

            let updateCommand (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"
                if func = "" then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1010, $"{func}", page, 0u)
                        
                getFunctionNUpdate (name, name, func,  true, page) |> ignore

            let updateOperator (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"
                if func = "" then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1010, $"{func}", page, 0u)
                        
                getFunctionNUpdate (name, name, func,   false,  page) |> ignore

            let updateBtn (row: Data.DataRow, btntype: BtnType, tableIO: Data.DataTable, page) =
                let btnName = $"{row.[(int) IOColumn.Name]}"
                match sys.HWButtons.Where(fun w -> w.ButtonType = btntype).TryFind(fun f -> f.Name = btnName.DeQuoteOnDemand()) with
                | Some btn ->
                    btn.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    btn.OutAddress <- $"{row.[(int) IOColumn.Output]}"
                    //ValidBtnAddress
                    let inaddr, outaddr =  getValidBtnAddress (btn)  Util.runtimeTarget
                    btn.InAddress  <-inaddr.Trim() 
                    btn.OutAddress <-outaddr.Trim() 

                    match getFunctionNUpdate (btn.Name, "", $"{row.[(int) IOColumn.Func]}",  false, page) with
                    |Some f -> btn.OperatorFunction <-  getOperatorFuntion f |> Some 
                    |_->()


                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1001, $"{btnName}", page, 0u)


            let updateLamp (row: Data.DataRow, lampType: LampType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"

                let lamps = sys.HWLamps.Where(fun w -> w.LampType = lampType)

                match lamps.TryFind(fun f -> f.Name = name.DeQuoteOnDemand()) with
                | Some lamp ->
                    lamp.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    lamp.OutAddress <- $"{row.[(int) IOColumn.Output]}"
                    //ValidBtnAddress
                    let inaddr, outaddr =  getValidLampAddress (lamp)   Util.runtimeTarget
                    lamp.InAddress  <-inaddr.Trim() 
                    lamp.OutAddress <-outaddr.Trim() 
                    match getFunctionNUpdate (lamp.Name, "", func,    false, page) with
                    |Some f ->    lamp.OperatorFunction <- getOperatorFuntion f |> Some 
                    |_->()

                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1002, $"{name}", page, 0u)

            let updateCondition (row: Data.DataRow, cType: ConditionType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"

                let conds = sys.HWConditions.Where(fun w -> w.ConditionType = cType)

                match conds.TryFind(fun f -> f.Name = name.DeQuoteOnDemand()) with
                | Some cond ->
                    cond.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    //ValidBtnAddress
                    let inaddr =  getValidCondiAddress (cond) Util.runtimeTarget
                    cond.InAddress  <-inaddr.Trim() 

                    match getFunctionNUpdate (cond.Name, "", func,   false, page) with
                    |Some f ->   cond.OperatorFunction <- getOperatorFuntion f |> Some 
                    |_->()

                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1007, $"{name}", page, 0u)

            dts
            |> Seq.iter (fun (page, dt) ->
                let tableIO = dt
              
                for row in tableIO.Rows do
                    let case = $"{row.[(int) IOColumn.Case]}"

                    if
                        (trimSpace $"{row.[(int) IOColumn.Name]}" <> "" //name 존재시만
                         && case <> $"{IOColumn.Case}") //header 스킵
                    then
                        match TextToXlsType(case) with
                        | XlsAddress -> updateDev (row, tableIO, page)
                        | XlsVariable ->    updateVar (row, tableIO, page)

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
