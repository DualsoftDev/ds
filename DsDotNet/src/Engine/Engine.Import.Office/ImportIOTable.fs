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

  


    let mutable inCnt = -1;
    let mutable outCnt = 63;
    let mutable memCnt = -1;
    
    let autoFillAddress(bInput:bool) =
        if bInput
        then 
            inCnt<-inCnt+1
            if RuntimeDS.Package.IsPackagePC() 
            then    ($"I{inCnt/16}.{inCnt%16}") 
            else    ($"%%IX0.{inCnt/64}.{inCnt%64}") //일단 LS 규격으로

        else 
            outCnt<-outCnt+1
            if RuntimeDS.Package.IsPackagePC() 
            then    ($"O{outCnt/16}.{outCnt%16}") 
            else    ($"%%QX0.{outCnt/64}.{outCnt%64}") //일단 LS 규격으로

    let errCheckTRX(trxCnt:int, name:string, addr:string)  =
        if   trxCnt=0  && addr <>"-"  then failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입필요." //0 이면 명시적으로 '-' 표기
        elif trxCnt<>0 && addr = "-"  then failwithf $"{name} 인터페이스 대상이 있으면 대쉬('-') 대신 실주소 기입필요."
       

    let errCheckNRetenAddress(trxCnt:int, addr:string, name,  bInput) =
            let addrNew = 
                if trxCnt=0 && addr.IsNullOrEmpty() 
                then "-" //"처음 자동할당은 인터페이스 대상이 없으면 대쉬('-') 자동 기입" 
                else addr  
            errCheckTRX  (trxCnt, name , addrNew)
            let address  = 
                if addrNew.IsNullOrEmpty()  && addrNew <> "-"
                then autoFillAddress bInput 
                else addrNew
            address.ToUpper()


    let getValidBtnAddress(btn:ButtonDef, bInput) =  
        if bInput
        then errCheckNRetenAddress (1, btn.InAddress, btn.Name, true)
        else errCheckNRetenAddress (1, btn.OutAddress, btn.Name, false)
    let getValidDevAddress(apiItem:ApiItem, name:string,  addr:string, bInput:bool) =  
        if bInput
        then errCheckNRetenAddress (apiItem.RXs.Count, addr, name, true)
        else errCheckNRetenAddress (apiItem.TXs.Count, addr, name, false)

    let getValidLampAddress(lamp:LampDef) =   errCheckNRetenAddress (1, lamp.OutAddress, lamp.Name, false)
    let getValidCondiAddress(cond:ConditionDef) =  errCheckNRetenAddress (1, cond.InAddress, cond.Name, true)

    let ApplyIO (sys: DsSystem, dts: (int * Data.DataTable) seq) =

        try
            let functionUpdate (name, funcText, funcs: HashSet<Func>, tableIO: Data.DataTable, isJob: bool, page) =
                funcs.Clear()

                if not <| ((trimSpace funcText) = "" || funcText = "-" || funcText = "↑") then
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

            let autoFillAddress (address:string)  =
                let addr = 
                        if address.Trim() = "" || address.Trim() = "-" then
                            ""
                        else
                            match RuntimeDS.Target with
                            | XGI -> if not <| address.StartsWith("%") then "%" + address else address
                            | _ -> address
                addr

            let updateDev (row: Data.DataRow, tableIO: Data.DataTable, page) =
               

                let devName = $"{row.[(int) IOColumn.Name]}"

                if not <| dicJob.ContainsKey(devName) then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._1006, $"{devName}", page, 0u)

                let dev = dicJob.[devName]
                let inAdd =    $"{row.[(int) IOColumn.Input]}".Trim()
                let outAdd =   $"{row.[(int) IOColumn.Output]}".Trim()

                errCheckTRX(dev.ApiItem.RXs.Count, devName, inAdd)
                errCheckTRX(dev.ApiItem.TXs.Count, devName, outAdd)

                dev.InAddress <- autoFillAddress inAdd
                dev.OutAddress <- autoFillAddress outAdd

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
                let variableData = VariableData(name, dataType, "-")
                sys.Variables.Add(variableData)

            let updateBtn (row: Data.DataRow, btntype: BtnType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let input = $"{row.[(int) IOColumn.Input]}" |> autoFillAddress
                let output = $"{row.[(int) IOColumn.Output]}" |> autoFillAddress
                let func = $"{row.[(int) IOColumn.Func]}"

                let btns = sys.SystemButtons.Where(fun w -> w.ButtonType = btntype)

                match btns.TryFind(fun f -> f.Name = name) with
                | Some btn ->
                    btn.InAddress <- input
                    btn.OutAddress <- output
                    functionUpdate (btn.Name, func, btn.Funcs, tableIO, false, page)
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1001, $"{name}", page, 0u)

            let updateLamp (row: Data.DataRow, lampType: LampType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let output = $"{row.[(int) IOColumn.Output]}" |> autoFillAddress
                let func = $"{row.[(int) IOColumn.Func]}"

                let lamps = sys.SystemLamps.Where(fun w -> w.LampType = lampType)

                match lamps.TryFind(fun f -> f.Name = name) with
                | Some lamp ->
                    lamp.OutAddress <- output
                    functionUpdate (lamp.Name, func, lamp.Funcs, tableIO, false, page)
                | None -> Office.ErrorPPT(ErrorCase.Name, ErrID._1002, $"{name}", page, 0u)

            let updateCondition (row: Data.DataRow, cType: ConditionType, tableIO: Data.DataTable, page) =
                let name = $"{row.[(int) IOColumn.Name]}"
                let output = $"{row.[(int) IOColumn.Input]}" |> autoFillAddress
                let func = $"{row.[(int) IOColumn.Func]}"

                let conds = sys.SystemConditions.Where(fun w -> w.ConditionType = cType)

                match conds.TryFind(fun f -> f.Name = name) with
                | Some cond ->
                    cond.InAddress <- output
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
