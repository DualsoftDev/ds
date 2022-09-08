// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System
open System.Data
open System.Collections.Concurrent
open System.Collections.Generic
open Microsoft.Office.Interop.Excel

[<AutoOpen>]
module ImportIOTable =

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
                Event.DoWork((int)(Convert.ToSingle(row + 1) / (rowCnt|>float32) * 50f));
                let newRow = dtResult.NewRow(); // DataTable에 새 행 할당
                for column in [|1..colCnt|] do
                    let data = workSheet.Cells.[row, column]  :?> Range
                    newRow.[column-1] <- data.Value2

                dtResult.Rows.Add(newRow) |> ignore
                
            workBook.Close();   // 워크북 닫기
            excelApp.Quit();        // 엑셀 어플리케이션 종료
            
            dtResult

        let tableIO = FromExcel(path)
        
        //Case	Flow	Name	    Type	Size	S(Output)	R(Output)	E(Input)
        //주소	P1	    AA	        I1	    bit	-	-	I9
        //주소	P1	    AA	        I2	    bit	-	-	I10
        //주소	S101	RBT3Right	IO	    bit	Q3	-	I13
        //주소	S101	RBT1Put	    O3	    bit	Q2	-	-
        //주소	S101	RBT2Left	IO	    bit	Q3	-	I16
        //주소	S101	RBT2Home	IO	    bit	Q2	-	I19
        
        sys.AddressSet.Clear();
        sys.VariableSet.Clear();
        sys.CommandSet.Clear();
        sys.ObserveSet.Clear();

        for row in tableIO.Rows do
            if($"{row.[2]}" = ""|>not) //name 존재시만
            then 
                match TagToType($"{row.[0]}") with
                |TagCase.Address  -> sys.AddressSet.TryAdd($"{row.[2]}", Tuple.Create($"{row.[5]}", $"{row.[6]}", $"{row.[7]}")) |>ignore
                |TagCase.Variable -> sys.VariableSet.TryAdd($"{row.[2]}", Type.DataToType($"{row.[4]}")) |>ignore
                |TagCase.Command -> sys.CommandSet.TryAdd($"{row.[2]}", $"{row.[5]}") |>ignore
                |TagCase.Observe -> sys.ObserveSet.TryAdd($"{row.[2]}", $"{row.[7]}") |>ignore
            
        for flow in sys.RootFlow()  do
            flow.CallSegments() |> Seq.append (flow.ExSegments())
            |> Seq.iter(fun seg -> 
                        let s, r, e = sys.AddressSet.[seg.Name]
                        seg.S <- s
                        seg.R <- r
                        seg.E <- e  )

        Event.DoWork(0);


            





