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
    | Case      = 0
    | Name      = 1
    | DataType  = 2
    | Input     = 3
    | Output    = 4
    | Job       = 5
    | Func      = 6

    let ApplyIO(sys:DsSystem, dt:Data.DataTable) =

        try
            let autoFillAddress(x:string) =
                if x.Trim() = "" then ""
                else
                    match RuntimeDS.Target with
                    |XGI -> if  not <| x.StartsWith("%")   then "%"+x else x
                    |_ ->   x

            let functionUpdate(funcText, funcs:HashSet<Func>, tableIO:Data.DataTable, isJob:bool) =
                funcs.Clear()
                if not <| ((trimSpace funcText) = "" || funcText = "-" ||  funcText = "↑")
                then getFunctions(funcText)
                        |> Seq.iter(fun (name, parms) ->
                            if (not<|isJob) && name <> "n"
                            then Office.ErrorXLS(ErrorCase.Name, ErrID._1005, $"{name}", tableIO.TableName)
                            funcs.Add(Func(name, parms)) |>ignore )
            
            let dicJob = sys.Jobs |> Seq.collect(fun f-> f.DeviceDefs) |> Seq.map(fun j->j.ApiName, j) |> dict
            let updateDev(row:Data.DataRow, tableIO:Data.DataTable) =
                let devName  = $"{row.[(int)IOColumn.Name]}"
                if not <| dicJob.ContainsKey(devName)
                then Office.ErrorXLS(ErrorCase.Name, ErrID._1006, tableIO.TableName,  $"{devName}.")
                let dev = dicJob.[devName]
                dev.InAddress  <- $"{row.[(int)IOColumn.Input]}" |> autoFillAddress
                dev.OutAddress <- $"{row.[(int)IOColumn.Output]}"|> autoFillAddress

                let jobName  = $"{row.[(int)IOColumn.Job]}"
                let func  = $"{row.[(int)IOColumn.Func]}"

                match sys.Jobs.TryFind(fun f-> f.Name = jobName) with
                | Some job -> 
                            let funcs = new HashSet<Func>()
                            functionUpdate (func, funcs, tableIO, true)
                            job.SetFuncs funcs
                | None -> if "↑" <> jobName //이름이 위와 같지 않은 경우
                            then Office.ErrorXLS(ErrorCase.Name, ErrID._1004, tableIO.TableName,  $"오류 이름 {jobName}.")

            let updateVar(row:Data.DataRow, tableIO:Data.DataTable) =
                let name      = $"{row.[(int)IOColumn.Name]}"
                let dataType  = $"{row.[(int)IOColumn.DataType]}" |> textToDataType
                let initValue = $"{row.[(int)IOColumn.Output]}"
                let variableData = VariableData(name, dataType, initValue)
                sys.Variables.Add(variableData)

            let updateBtn(row:Data.DataRow, btntype:BtnType, tableIO:Data.DataTable) =
                let name  = $"{row.[(int)IOColumn.Name]}"
                let input = $"{row.[(int)IOColumn.Input]}"  |> autoFillAddress
                let output= $"{row.[(int)IOColumn.Output]}" |> autoFillAddress
                let func  = $"{row.[(int)IOColumn.Func]}"

                let btns = sys.SystemButtons.Where(fun w->w.ButtonType = btntype)
                match btns.TryFind(fun f -> f.Name = name) with
                | Some btn -> btn.InAddress <- input
                              btn.OutAddress <- output
                              functionUpdate (func, btn.Funcs, tableIO, false)
                | None -> Office.ErrorXLS(ErrorCase.Name, ErrID._1001, $"{name}", tableIO.TableName)

            let updateLamp(row:Data.DataRow, lampType:LampType, tableIO:Data.DataTable) =
                let name  = $"{row.[(int)IOColumn.Name]}"
                let output= $"{row.[(int)IOColumn.Output]}" |> autoFillAddress
                let func  = $"{row.[(int)IOColumn.Func]}"

                let lamps = sys.SystemLamps.Where(fun w->w.LampType = lampType)
                match lamps.TryFind(fun f -> f.Name = name) with
                | Some lamp -> lamp.OutAddress <- output
                               functionUpdate (func, lamp.Funcs, tableIO, false)
                | None -> Office.ErrorXLS(ErrorCase.Name, ErrID._1002, $"{name}", tableIO.TableName)

            let updateCondition (row:Data.DataRow, cType:ConditionType, tableIO:Data.DataTable) =
                let name  = $"{row.[(int)IOColumn.Name]}"
                let output= $"{row.[(int)IOColumn.Input]}" |> autoFillAddress
                let func  = $"{row.[(int)IOColumn.Func]}"

                let conds = sys.SystemConditions.Where(fun w->w.ConditionType = cType)
                match conds.TryFind(fun f -> f.Name = name) with
                | Some cond -> cond.InAddress <- output
                               functionUpdate (func, cond.Funcs, tableIO, false)
                | None -> Office.ErrorXLS(ErrorCase.Name, ErrID._1002, $"{name}", tableIO.TableName)

          
            let tableIO = dt
            for row in tableIO.Rows do
                let case = $"{row.[(int)IOColumn.Case]}"
                if(trimSpace $"{row.[(int)IOColumn.Name]}" <> ""//name 존재시만
                && case <> $"{IOColumn.Case}") //header 스킵
                then
                    match TextToXlsType(case) with
                    | XlsAddress        -> updateDev(row, tableIO)
                    | XlsVariable       -> updateVar(row, tableIO)

                    | XlsAutoBTN        -> updateBtn  (row, BtnType.DuAutoBTN             , tableIO)
                    | XlsManualBTN      -> updateBtn  (row, BtnType.DuManualBTN           , tableIO)
                    | XlsDriveBTN       -> updateBtn  (row, BtnType.DuDriveBTN            , tableIO)
                    | XlsStopBTN        -> updateBtn  (row, BtnType.DuStopBTN             , tableIO)
                    | XlsEmergencyBTN   -> updateBtn  (row, BtnType.DuEmergencyBTN        , tableIO)
                    | XlsTestBTN        -> updateBtn  (row, BtnType.DuTestBTN             , tableIO)
                    | XlsReadyBTN       -> updateBtn  (row, BtnType.DuReadyBTN            , tableIO)
                    | XlsClearBTN       -> updateBtn  (row, BtnType.DuClearBTN            , tableIO)
                    | XlsHomeBTN        -> updateBtn  (row, BtnType.DuHomeBTN             , tableIO)

                    | XlsAutoLamp       -> updateLamp (row, LampType.DuAutoLamp       , tableIO)
                    | XlsManualLamp     -> updateLamp (row, LampType.DuManualLamp     , tableIO)
                    | XlsDriveLamp      -> updateLamp (row, LampType.DuDriveLamp      , tableIO)
                    | XlsStopLamp       -> updateLamp (row, LampType.DuStopLamp       , tableIO)
                    | XlsEmergencyLamp  -> updateLamp (row, LampType.DuEmergencyLamp  , tableIO)
                    | XlsTestLamp       -> updateLamp (row, LampType.DuTestDriveLamp  , tableIO)
                    | XlsReadyLamp      -> updateLamp (row, LampType.DuReadyLamp      , tableIO)
                    | XlsIdleLamp       -> updateLamp (row, LampType.DuIdleLamp       , tableIO)

                    | XlsConditionReady -> updateCondition (row, ConditionType.DuReadyState , tableIO)
                    | XlsConditionDrive -> updateCondition (row, ConditionType.DuDriveState , tableIO)
       
        with ex ->  failwithf  $"{ex.Message}"








