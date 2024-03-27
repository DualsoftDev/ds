// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic

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

    let ApplyIO (sys: DsSystem, dts: (int * Data.DataTable) seq) =

        try
            
            let getFunction (name, funcText, tableIO: Data.DataTable, isJob: bool, page) =
                let funcs = HashSet<Func>()
                if not <| ((trimSpace funcText) = "" || funcText = TextSkip || funcText = TextFuncNotUsed) then
                    let funTexts = getFunctions (funcText)
                    if funTexts.length() > 1 then 
                        Office.ErrorPPT(ErrorCase.Name, ErrID._1008, $"{name}", page, 0u)

                    funTexts |> Seq.iter (fun (funName, parms) ->
                        if (not <| isJob) && funName <> "n" then
                            Office.ErrorPPT(ErrorCase.Name, ErrID._1005, $"{funName}", page, 0u)

                        funcs.Add(Func(funName, parms)) |> ignore)
                if funcs.any() then Some (funcs.Head()) else None

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


            let updateDev (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let devName = $"{row.[(int) IOColumn.Name]}"

                if not <| dicDev.ContainsKey(devName) then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1006, $"{devName}", page, 0u)

                let dev = dicDev.[devName]
                let inAdd =    $"{row.[(int) IOColumn.Input]}".Trim() |>emptyToSkipAddress
                let outAdd =   $"{row.[(int) IOColumn.Output]}".Trim()|>emptyToSkipAddress
              
                dev.InAddress  <-  getValidAddress(inAdd,   dev.QualifiedName, false, IOType.In)
                dev.OutAddress <-  getValidAddress(outAdd,  dev.QualifiedName, false, IOType.Out)


                let job = dicJob[devName]
            
                let func = $"{row.[(int) IOColumn.Func]}"
                let func = getFunction (job.Name, func, tableIO, true, page)
                if func.IsSome
                then 
                    if job.Func.IsSome 
                    then
                        Office.ErrorPPT(ErrorCase.Group, ErrID._1009, $"{devName}", page, 0u)
                    else 
                        job.Func <- func
                        
             
            let updateVar (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let dataType = $"{row.[(int) IOColumn.DataType]}" |> textToDataType
                let variableData = VariableData(name, dataType, TextAddrEmpty)
                sys.Variables.Add(variableData)

            let updateBtn (row: Data.DataRow, btntype: BtnType, tableIO: Data.DataTable, page) =
                let btnName = $"{row.[(int) IOColumn.Name]}"
                match sys.HWButtons.Where(fun w -> w.ButtonType = btntype).TryFind(fun f -> f.Name = btnName.DeQuoteOnDemand()) with
                | Some btn ->
                    btn.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    btn.OutAddress <- $"{row.[(int) IOColumn.Output]}"
                    //ValidBtnAddress
                    let inaddr, outaddr =  getValidBtnAddress (btn)
                    btn.InAddress  <-inaddr.Trim() 
                    btn.OutAddress <-outaddr.Trim() 

                    btn.Func <- getFunction (btn.Name, $"{row.[(int) IOColumn.Func]}", tableIO, false, page)
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
                    let inaddr, outaddr =  getValidLampAddress (lamp)
                    lamp.InAddress  <-inaddr.Trim() 
                    lamp.OutAddress <-outaddr.Trim() 
                    lamp.Func <- getFunction (lamp.Name, func,  tableIO, false, page)
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1002, $"{name}", page, 0u)

            let updateCondition (row: Data.DataRow, cType: ConditionType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"

                let conds = sys.HWConditions.Where(fun w -> w.ConditionType = cType)

                match conds.TryFind(fun f -> f.Name = name.DeQuoteOnDemand()) with
                | Some cond ->
                    cond.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    //ValidBtnAddress
                    let inaddr =  getValidCondiAddress (cond)
                    cond.InAddress  <-inaddr.Trim() 
                    cond.Func <- getFunction (cond.Name, func, tableIO, false, page)
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
                        | XlsVariable -> updateVar (row, tableIO, page)

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

                        | XlsConditionReady -> updateCondition (row, ConditionType.DuReadyState, tableIO, page)

            )

        with ex ->
            failwithf $"{ex.Message}"
