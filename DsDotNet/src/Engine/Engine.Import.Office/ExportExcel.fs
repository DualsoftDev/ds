// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Data
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet

[<AutoOpen>]
module ExportExcelModule =

  
    let createSpreadsheet (filepath:string) (tables:DataTable seq) (colWidth:float) (showColumnHead:bool) =
        let seenNames = System.Collections.Generic.HashSet<string>()
        for dt in tables do
            if not (seenNames.Add dt.TableName) then
                failwithf "Duplicate table name found: %s" dt.TableName

        let createDataSheets (dts: seq<DataTable>) (showColumnHead: bool) =
            dts |> Seq.map(fun dt ->
                let sheetData = new SheetData()

                // Conditionally add column headers based on the showColumnHead parameter
                let headerRow = new Row()
                for col in dt.Columns do
                    let cell = new Cell()
                    cell.DataType <- CellValues.String
                    cell.CellValue <-new CellValue(col.ColumnName) 
                                  
                    if showColumnHead 
                    then headerRow.AppendChild(cell :> OpenXmlElement) |> ignore
                sheetData.AppendChild(headerRow :> OpenXmlElement) |> ignore

                let totalRowCnt = dt.Rows.Count - 1
                // Populate the sheet data with data from the DataTable
                for rowIndex in 0 .. totalRowCnt do
                    let dataRow = new Row()
                    for colIndex in 0 .. dt.Columns.Count - 1 do
                        let cell = new Cell()
                        let value = dt.Rows.[rowIndex].[colIndex]
                        match value with
                        | :? string as strValue ->
                            cell.DataType <- CellValues.String
                            cell.CellValue <- new CellValue(strValue)
                        | :? System.IConvertible as convValue ->
                            cell.DataType <- CellValues.Number
                            cell.CellValue <- new CellValue(convValue.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        | _ ->
                            // Handle other types or throw an exception
                            ()
                        let columnName = char (65 + colIndex) |> string // Convert column index to Excel column letter (A, B, C, ...)
                        cell.CellReference <- new StringValue(sprintf "%s%d" columnName (rowIndex + 2)) // Row index starts from 1, so add 2
                        dataRow.AppendChild(cell :> OpenXmlElement) |> ignore

                    sheetData.AppendChild(dataRow :> OpenXmlElement) |> ignore

                dt.TableName, sheetData
            )

        let sheetDataSeq = createDataSheets tables showColumnHead

        // Create a spreadsheet document by supplying the filepath.
        // By default, AutoSave = true, Editable = true, and Type = xlsx.
        use spreadsheetDocument = SpreadsheetDocument.Create(filepath, SpreadsheetDocumentType.Workbook)

        // Add a WorkbookPart to the document.
        let workbookPart = spreadsheetDocument.AddWorkbookPart()
        workbookPart.Workbook <- new Workbook()

        // Add Sheets to the Workbook.
        let sheets = workbookPart.Workbook.AppendChild<Sheets>(new Sheets())

        // Counter for assigning unique SheetId
        let mutable sheetId = 1u

        // Function to add a worksheet for each SheetData item
        sheetDataSeq
        |> Seq.iter (fun (sheetName, sheetData) ->
            let worksheetPart = workbookPart.AddNewPart<WorksheetPart>()
            worksheetPart.Worksheet <- new Worksheet(sheetData :> OpenXmlElement)

            // Set the column width
            let columns = new Columns()
            let column = new Column(Min = 1u, Max = 16384u, Width = colWidth, CustomWidth = true)
            columns.Append(column:> OpenXmlElement)
        
            worksheetPart.Worksheet.InsertAt(columns :> OpenXmlElement, 0) |> ignore

            // Append a new worksheet and associate it with the workbook.
            let sheet = new Sheet(Id = StringValue(workbookPart.GetIdOfPart(worksheetPart)),
                                  SheetId = UInt32Value(sheetId),
                                  Name = StringValue(sheetName))

            sheets.Append(sheet :> OpenXmlElement) |> ignore

            // Increment the sheetId for the next sheet
            sheetId <- sheetId + 1u
        )

        // Save the changes to the workbook
        workbookPart.Workbook.Save()
