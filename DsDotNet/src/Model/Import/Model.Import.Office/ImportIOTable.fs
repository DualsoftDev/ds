// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open Microsoft.Office.Interop.Excel
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module ImportIOTable =
       
    type IOColumn =
    | Case      = 0
    | Name      = 1
    | Type      = 2
    | Size      = 3
    | Output    = 4
    | Input     = 5
    | Command   = 6
    | Observe   = 7

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
            systems
            |> Seq.iter(fun sys -> 
                let tableIOs = dataset.Tables
                                |> Seq.cast<System.Data.DataTable>
                                |> Seq.filter(fun tb -> tb.TableName = sys.Name)
                
                if tableIOs.length()  = 0
                then Office.ErrorPPT(ErrorCase.Path, $"{sys.Name}에 해당하는 엑셀 Sheet 가 없습니다.", "", 0, "")

                let tableIO = tableIOs |> Seq.head
                let dicJob = sys.Jobs |> Seq.collect(fun f-> f.JobDefs) |> Seq.map(fun j->j.ApiName, j) |> dict
                for row in tableIO.Rows do
                    if($"{row.[(int)IOColumn.Name]}" = ""|>not && $"{row.[(int)IOColumn.Name]}" = "-"|>not) //name 존재시만
                    then 
                        match ExcelCaseToType($"{row.[(int)IOColumn.Case]}") with
                        |ExcelCase.ExcelAddress  -> 
                            let jobDef = dicJob.[$"{row.[(int)IOColumn.Name]}"]
                            jobDef.InTag  <- $"{row.[(int)IOColumn.Input]}"
                            jobDef.OutTag <- $"{row.[(int)IOColumn.Output]}"

                        |ExcelCase.ExcelVariable -> 
                            let name      = $"{row.[(int)IOColumn.Name]}"
                            let datatype  = $"{row.[(int)IOColumn.Type]}"
                            let initValue = $"{row.[(int)IOColumn.Output]}"
                            let variableData =  $"{datatype} {name} = {initValue}"
                            sys.OriginalCodeBlocks.Add(variableData)

                        |ExcelCase.ExcelCommand ->  
                            let jobDef = dicJob.[$"{row.[(int)IOColumn.Name]}"]
                            jobDef.CommandOutTimming  <- $"{row.[(int)IOColumn.Command]}"

                        |ExcelCase.ExcelObserve ->  
                            let jobDef = dicJob.[$"{row.[(int)IOColumn.Name]}"]
                            jobDef.ObserveInTimming   <- $"{row.[(int)IOColumn.Observe]}"

                        |ExcelCase.ExcelStartBTN        
                        |ExcelCase.ExcelResetBTN        
                        |ExcelCase.ExcelAutoBTN         
                        |ExcelCase.ExcelEmergencyBTN   ->  ()// 버튼 TAG 구현 필요  //test ahn todo
            )
        with ex ->  failwithf  $"{ex.Message}"
        DoWork(0);


            





