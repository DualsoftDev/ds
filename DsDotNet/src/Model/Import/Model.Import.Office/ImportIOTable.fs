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
    | Flow     = 1
    | Name      = 2
    | Type      = 3
    | Size      = 4
    | Output    = 5
    | Input     = 6
    | Command   = 7
    | Observe   = 8

    let ApplyExcel(path:string, sys:DsSystem) =
        let FromExcel(path:string) =
            
            let excelApp = new ApplicationClass(Visible = false)
            let workBook = excelApp.Workbooks.Open(path);                       // 워크북 열기
            let workSheet = workBook.Worksheets.get_Item(1)  :?> Worksheet // 엑셀 첫번째 워크시트 가져오기
            
            // 사용중인 셀 범위를 가져오기
            let rowCnt = workSheet.UsedRange.Rows.Count
            let colCnt = workSheet.UsedRange.Columns.Count

            let dtResult = new System.Data.DataTable()

            
            for column in [|1..colCnt|] do
                dtResult.Columns.Add($"{column}") |> ignore

            for row in [|2..rowCnt|] do
                DoWork((int)(Convert.ToSingle(row + 1) / (rowCnt|>float32) * 50f));
                let newRow = dtResult.NewRow(); // DataTable에 새 행 할당
                for column in [|1..colCnt|] do
                    let data = workSheet.Cells.[row, column]  :?> Range
                    newRow.[column-1] <- data.Value2

                dtResult.Rows.Add(newRow) |> ignore
                
            workBook.Close();   // 워크북 닫기
            excelApp.Quit();        // 엑셀 어플리케이션 종료
            
            dtResult

        let tableIO = FromExcel(path)
        
        //Case	MFlow	Name	    Type	Size	S(Output)	R(Output)	E(Input)
        //주소	P1	    AA	        I1	    bit	-	-	I9
        //주소	P1	    AA	        I2	    bit	-	-	I10
        //주소	S101	RBT3Right	IO	    bit	Q3	-	I13
        //주소	S101	RBT1Put	    O3	    bit	Q2	-	-
        //주소	S101	RBT2Left	IO	    bit	Q3	-	I16
        //주소	S101	RBT2Home	IO	    bit	Q2	-	I19
        
        sys.Variables.Clear();
        sys.Commands.Clear();
        sys.Observes.Clear();
        try
            let dicJob = sys.Jobs |> Seq.collect(fun f-> f.JobDefs) |> Seq.map(fun j->j.ApiName, j) |> dict
            for row in tableIO.Rows do
                if($"{row.[2]}" = ""|>not && $"{row.[2]}" = "-"|>not) //name 존재시만
                then 
                    match TagToType($"{row.[0]}") with
                    |TagCase.Address  -> 
                        let name   = $"{row.[(int)IOColumn.Name]}"
                        let inTag  = $"{row.[(int)IOColumn.Input]}"
                        let outTag = $"{row.[(int)IOColumn.Output]}"
                        dicJob.[name].InTag  <- inTag
                        dicJob.[name].OutTag <-outTag

                    |TagCase.Variable ->    //ahn 초기값 입력
                        let item = CodeElements.Variable($"{row.[(int)IOColumn.Name]}", $"{row.[(int)IOColumn.Size]}",  "")
                        sys.Variables.Add(item) |>ignore
                    |TagCase.Command  ->        //ahn 수식 입력
                        let item = CodeElements.Command($"{row.[(int)IOColumn.Command]}", FunctionApplication("", [||]))
                        sys.Commands.Add(item) |>ignore
                    |TagCase.Observe  ->        //ahn 수식 입력
                        let item = CodeElements.Observe($"{row.[(int)IOColumn.Observe]}", FunctionApplication("", [||]))
                        sys.Observes.Add(item) |>ignore

        with ex ->  failwithf  $"{ex.Message}"
        DoWork(0);


            





