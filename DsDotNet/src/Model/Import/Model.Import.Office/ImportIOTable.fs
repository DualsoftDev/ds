// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open Microsoft.Office.Interop.Excel
open Engine.Common.FS
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module ImportIOTable =
       
    type IOColumn =
    | Case      = 0
    | Name      = 1
    | DataType  = 2
    | Input     = 3
    | Output    = 4
    | Job       = 5
    | Func      = 6

    let ApplyExcel(path:string, systems:DsSystem seq) =
        let sys = systems.Head()
        let FromExcel(path:string) =
            let dataset = new System.Data.DataSet()
            let excelApp = new ApplicationClass(Visible = false)
                    // 워크북 열기
            let workBook = excelApp.Workbooks.Open(path);       
            
            workBook.Worksheets 
            |> Seq.cast<Worksheet>
            |> Seq.iter(fun workSheet -> 
                let rowCnt = workSheet.UsedRange.Rows.Count
                let colCnt = workSheet.UsedRange.Columns.Count
            
                let dtResult = dataset.Tables.Add()
                dtResult.TableName <- workSheet.Name
            
                for column in [|1..colCnt|] do
                    dtResult.Columns.Add($"{column}") |> ignore

                for row in [|2..rowCnt|] do
                    DoWork((int)(Convert.ToSingle(row + 1) / (rowCnt|>float32) * 50f));
                    let newRow = dtResult.NewRow(); // DataTable에 새 행 할당
                    for column in [|1..colCnt|] do
                        let data = workSheet.Cells.[row, column]  :?> Range
                        newRow.[column-1] <- data.Value2

                    dtResult.Rows.Add(newRow) |> ignore
            )
            //    let workSheet = workBook.Worksheets.get_Item(1)  :?> Worksheet // 엑셀 첫번째 워크시트 가져오기
            // 사용중인 셀 범위를 가져오기
            workBook.Close();   // 워크북 닫기
            excelApp.Quit();        // 엑셀 어플리케이션 종료
            
            dataset

        let dataset = FromExcel(path)
        try
            let functionUpdate(funcText, funcs:HashSet<Func>, tableIO:Data.DataTable, isJob:bool) = 
                funcs.Clear()
                if funcText <> "" && funcText <> "-"
                then getFunctions(funcText) 
                        |> Seq.iter(fun (name, parms) -> 
                            if (not<|isJob) && name <> "n"
                            then Office.ErrorXLS(ErrorCase.Name, ErrID._1005, $"{name}", tableIO.TableName, path)
                            funcs.Add(Func(name, parms)) |>ignore )

            let updateBtn(row:Data.DataRow, btntype:BtnType, tableIO:Data.DataTable) = 
                let name  = $"{row.[(int)IOColumn.Name]}"
                let input = $"{row.[(int)IOColumn.Input]}"
                let output= $"{row.[(int)IOColumn.Output]}"
                let func  = $"{row.[(int)IOColumn.Func]}"

                let btns = sys.SystemButtons.Where(fun w->w.ButtonType = btntype)
                match btns.TryFind(fun f -> f.Name = name) with
                | Some btn -> btn.InAddress <- input
                              btn.OutAddress <- output
                              functionUpdate (func, btn.Funcs, tableIO, false)
                | None -> Office.ErrorXLS(ErrorCase.Name, ErrID._1001, $"{name}", tableIO.TableName, path)

            let updateLamp(row:Data.DataRow, lampType:LampType, tableIO:Data.DataTable) = 
                let name  = $"{row.[(int)IOColumn.Name]}"
                let output= $"{row.[(int)IOColumn.Output]}"
                let func  = $"{row.[(int)IOColumn.Func]}"

                let lamps = sys.SystemLamps.Where(fun w->w.LampType = lampType)
                match lamps.TryFind(fun f -> f.Name = name) with
                | Some lamp -> lamp.OutAddress <- output
                               functionUpdate (func, lamp.Funcs, tableIO, false)
                | None -> Office.ErrorXLS(ErrorCase.Name, ErrID._1002, $"{name}", tableIO.TableName, path)
            
            let updateCondition (row:Data.DataRow, cType:ConditionType, tableIO:Data.DataTable) = 
                let name  = $"{row.[(int)IOColumn.Name]}"
                let output= $"{row.[(int)IOColumn.Output]}"
                let func  = $"{row.[(int)IOColumn.Func]}"

                let conds = sys.SystemConditions.Where(fun w->w.ConditionType = cType)
                match conds.TryFind(fun f -> f.Name = name) with
                | Some cond -> cond.InAddress <- output
                               functionUpdate (func, cond.Funcs, tableIO, false)
                | None -> Office.ErrorXLS(ErrorCase.Name, ErrID._1002, $"{name}", tableIO.TableName, path)

            systems
            |> Seq.iter(fun sys -> 
                let tableIOs = dataset.Tables
                                |> Seq.cast<System.Data.DataTable>
                                |> Seq.filter(fun tb -> tb.TableName = sys.Name)
                
                if tableIOs.length()  = 0
                then Office.ErrorXLS(ErrorCase.Name, ErrID._1003, "",  $"오류 이름 {sys.Name}.")

                let tableIO = tableIOs |> Seq.head
                let dicJob = sys.Jobs |> Seq.collect(fun f-> f.JobDefs) |> Seq.map(fun j->j.ApiName, j) |> dict
                for row in tableIO.Rows do
                    if($"{row.[(int)IOColumn.Name]}" = ""|>not && $"{row.[(int)IOColumn.Name]}" = "-"|>not) //name 존재시만
                    then 
                        match TextToXlsType($"{row.[(int)IOColumn.Case]}") with
                        | XlsAddress  -> 
                            let jobDef = dicJob.[$"{row.[(int)IOColumn.Name]}"]
                            jobDef.InAddress  <- $"{row.[(int)IOColumn.Input]}"
                            jobDef.OutAddress <- $"{row.[(int)IOColumn.Output]}"
                            
                            let jobName  = $"{row.[(int)IOColumn.Job]}"
                            let func  = $"{row.[(int)IOColumn.Func]}"

                            match sys.Jobs.TryFind(fun f-> f.Name = jobName) with
                            | Some job ->  functionUpdate (func, job.Funcs, tableIO, true)
                            | None -> if "↑" <> jobName //이름이 위와 같지 않은 경우
                                      then Office.ErrorXLS(ErrorCase.Name, ErrID._1004, tableIO.TableName,  $"오류 이름 {jobName}.")

                        | XlsVariable -> 
                            let name      = $"{row.[(int)IOColumn.Name]}" 
                            let dataType  = $"{row.[(int)IOColumn.DataType]}" |> DataToType
                            let initValue = $"{row.[(int)IOColumn.Output]}"
                            let variableData = VariableData(name, dataType, initValue)
                            sys.Variables.Add(variableData)

                        | XlsAutoBTN           -> updateBtn  (row, BtnType.DuAutoBTN             , tableIO)
                        | XlsManualBTN         -> updateBtn  (row, BtnType.DuManualBTN           , tableIO)
                        | XlsDriveBTN          -> updateBtn  (row, BtnType.DuDriveBTN            , tableIO)
                        | XlsStopBTN           -> updateBtn  (row, BtnType.DuStopBTN             , tableIO)
                        | XlsEmergencyBTN      -> updateBtn  (row, BtnType.DuEmergencyBTN        , tableIO)
                        | XlsTestBTN           -> updateBtn  (row, BtnType.DuTestBTN             , tableIO)
                        | XlsReadyBTN          -> updateBtn  (row, BtnType.DuReadyBTN            , tableIO)
                        | XlsClearBTN          -> updateBtn  (row, BtnType.DuClearBTN            , tableIO)
                        | XlsHomeBTN           -> updateBtn  (row, BtnType.DuHomeBTN             , tableIO)

                        | XlsAutoModeLamp      -> updateLamp (row, LampType.DuAutoModeLamp       , tableIO)
                        | XlsManualModeLamp    -> updateLamp (row, LampType.DuManualModeLamp     , tableIO)
                        | XlsDriveModeLamp     -> updateLamp (row, LampType.DuDriveModeLamp      , tableIO)
                        | XlsStopModeLamp      -> updateLamp (row, LampType.DuStopModeLamp       , tableIO)
                        | XlsEmergencyModeLamp -> updateLamp (row, LampType.DuEmergencyModeLamp  , tableIO)
                        | XlsTestModeLamp      -> updateLamp (row, LampType.DuTestModeLamp       , tableIO)
                        | XlsReadyModeLamp     -> updateLamp (row, LampType.DuReadyModeLamp      , tableIO)
                        | XlsConditionReady    -> updateCondition (row, ConditionType.DuReadyState , tableIO)
                        | XlsConditionDrive    -> updateCondition (row, ConditionType.DuDriveState , tableIO)
            )
        with ex ->  failwithf  $"{ex.Message}"
        DoWork(0);


            





