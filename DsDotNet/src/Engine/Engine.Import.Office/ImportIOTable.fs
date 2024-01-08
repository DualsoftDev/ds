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
        | Job = 5
        | Func = 6

    let ApplyIO (sys: DsSystem, dts: (int * Data.DataTable) seq) =

        try
            let functionUpdate (name, funcText, funcs: HashSet<Func>, tableIO: Data.DataTable, isJob: bool, page) =
                funcs.Clear()

                if not <| ((trimSpace funcText) = "" || funcText = TextSkip || funcText = "↑") then
                    getFunctions (funcText)
                    |> Seq.iter (fun (name, parms) ->
                        if (not <| isJob) && name <> "n" then
                            Office.ErrorPPT(ErrorCase.Name, ErrID._1005, $"{name}", page, 0u)

                        funcs.Add(Func(name, parms)) |> ignore)

            let dicJob =
                sys.Jobs
                |> Seq.collect (fun f -> f.DeviceDefs)
                |> Seq.map (fun j -> j.ApiName, j)
                |> dict

            let chageParserText newAddr = if newAddr= TextSkip then TextAddrEmpty else newAddr
            let changeValidAddress (address:string)  =
    
                    let address = address.Trim() 
                    
                    address |> chageParserText

            let updateDev (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let devName = $"{row.[(int) IOColumn.Name]}"

                if not <| dicJob.ContainsKey(devName) then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1006, $"{devName}", page, 0u)

                let dev = dicJob.[devName]
                let inAdd =    $"{row.[(int) IOColumn.Input]}".Trim()
                let outAdd =   $"{row.[(int) IOColumn.Output]}".Trim()

                if outAdd.ToUpper().Contains("%MX80001") then ()
               

                dev.InAddress <-  changeValidAddress inAdd
                dev.OutAddress <- changeValidAddress outAdd
                dev.InAddress <-  getValidDevAddress (dev, true) |> changeValidAddress
                dev.OutAddress <- getValidDevAddress (dev, false)|> changeValidAddress

                let jobName = $"{row.[(int) IOColumn.Job]}"
                let func = $"{row.[(int) IOColumn.Func]}"

                match sys.Jobs.TryFind(fun f -> f.Name = jobName) with
                | Some job ->
                    let funcs = new HashSet<Func>()
                    functionUpdate (job.Name, func, funcs, tableIO, true, page)
                    job.SetFuncs funcs
                | None ->
                    if
                        "↑" <> jobName //이름이 위와 같지 않은 경우
                    then
                        Office.ErrorPPT(ErrorCase.Name, ErrID._1004, $"오류 이름 {jobName}.", page, 0u)

            let updateVar (row: Data.DataRow, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let dataType = $"{row.[(int) IOColumn.DataType]}" |> textToDataType
                let variableData = VariableData(name, dataType, TextAddrEmpty)
                sys.Variables.Add(variableData)

            let updateBtn (row: Data.DataRow, btntype: BtnType, tableIO: Data.DataTable, page) =
                let btnName = $"{row.[(int) IOColumn.Name]}"
                match sys.HWButtons.Where(fun w -> w.ButtonType = btntype).TryFind(fun f -> f.Name = btnName) with
                | Some btn ->
                    btn.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    btn.OutAddress <- $"{row.[(int) IOColumn.Output]}"
                    //ValidBtnAddress
                    let inaddr, outaddr =  getValidBtnAddress (btn)
                    btn.InAddress  <-inaddr |> changeValidAddress
                    btn.OutAddress <-outaddr|> changeValidAddress
                    functionUpdate (btn.Name, $"{row.[(int) IOColumn.Func]}", btn.Funcs, tableIO, false, page)
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1001, $"{btnName}", page, 0u)


            let updateLamp (row: Data.DataRow, lampType: LampType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"

                let lamps = sys.HWLamps.Where(fun w -> w.LampType = lampType)

                match lamps.TryFind(fun f -> f.Name = name) with
                | Some lamp ->
                    lamp.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    lamp.OutAddress <- $"{row.[(int) IOColumn.Output]}"
                    //ValidBtnAddress
                    let inaddr, outaddr =  getValidLampAddress (lamp)
                    lamp.InAddress  <-inaddr|> changeValidAddress
                    lamp.OutAddress <-outaddr|> changeValidAddress
                    functionUpdate (lamp.Name, func, lamp.Funcs, tableIO, false, page)
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1002, $"{name}", page, 0u)

            let updateCondition (row: Data.DataRow, cType: ConditionType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let func = $"{row.[(int) IOColumn.Func]}"

                let conds = sys.HWSystemConditions.Where(fun w -> w.ConditionType = cType)

                match conds.TryFind(fun f -> f.Name = name) with
                | Some cond ->
                    cond.InAddress  <- $"{row.[(int) IOColumn.Input]}" 
                    cond.OutAddress <- $"{row.[(int) IOColumn.Output]}"
                    //ValidBtnAddress
                    let inaddr, outaddr =  getValidCondiAddress (cond)
                    cond.InAddress  <-inaddr |> changeValidAddress
                    cond.OutAddress <-outaddr  |> changeValidAddress    
                    functionUpdate (cond.Name, func, cond.Funcs, tableIO, false, page)
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1002, $"{name}", page, 0u)

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
                        | XlsStopBTN -> updateBtn (row, BtnType.DuStopBTN, tableIO, page)
                        | XlsEmergencyBTN -> updateBtn (row, BtnType.DuEmergencyBTN, tableIO, page)
                        | XlsTestBTN -> updateBtn (row, BtnType.DuTestBTN, tableIO, page)
                        | XlsReadyBTN -> updateBtn (row, BtnType.DuReadyBTN, tableIO, page)
                        | XlsClearBTN -> updateBtn (row, BtnType.DuClearBTN, tableIO, page)
                        | XlsHomeBTN -> updateBtn (row, BtnType.DuHomeBTN, tableIO, page)

                        | XlsAutoLamp -> updateLamp (row, LampType.DuAutoLamp, tableIO, page)
                        | XlsManualLamp -> updateLamp (row, LampType.DuManualLamp, tableIO, page)
                        | XlsDriveLamp -> updateLamp (row, LampType.DuDriveLamp, tableIO, page)
                        | XlsStopLamp -> updateLamp (row, LampType.DuStopLamp, tableIO, page)
                        | XlsEmergencyLamp -> updateLamp (row, LampType.DuEmergencyLamp, tableIO, page)
                        | XlsTestLamp -> updateLamp (row, LampType.DuTestDriveLamp, tableIO, page)
                        | XlsReadyLamp -> updateLamp (row, LampType.DuReadyLamp, tableIO, page)
                        | XlsIdleLamp -> updateLamp (row, LampType.DuIdleLamp, tableIO, page)

                        | XlsConditionReady -> updateCondition (row, ConditionType.DuReadyState, tableIO, page)
                        | XlsConditionDrive -> updateCondition (row, ConditionType.DuDriveState, tableIO, page)

            )

        with ex ->
            failwithf $"{ex.Message}"
