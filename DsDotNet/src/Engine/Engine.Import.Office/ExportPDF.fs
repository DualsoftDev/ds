namespace Engine.Import.Office

open System
open System.Data
open System.Drawing
open Spire.Pdf
open Spire.Pdf.Graphics
open Spire.Pdf.Tables

[<AutoOpen>]
module ExportToPdfModule =

    let convertDataSetToPdf (pdfFilePath: string) (dataTables: seq<DataTable>) =
     
        // Create a new PDF document
        let doc = new PdfDocument()

        // Iterate through each DataTable in the sequence
        for dataTable in dataTables do
            // Add a new page for the current DataTable
            let page = doc.Pages.Add(PdfPageSize.A4, new PdfMargins(40f))

            // Set title font
            let titleFont =  new PdfTrueTypeFont("¸¼Àº °íµñ", 14f, PdfFontStyle.Regular, true )

            // Draw the table title
            let title = if String.IsNullOrWhiteSpace(dataTable.TableName) then "Table" else dataTable.TableName
            page.Canvas.DrawString(title, titleFont, PdfBrushes.Black, PointF(0f, 0f))

            // Adjust the starting point for the table to be below the title
            let tableStartY = titleFont.MeasureString(title).Height + 10f // Add some margin after the title

            // Create a new PdfTable and set its DataSource to the current DataTable
            let table = new PdfTable()
            table.DataSource <- dataTable

            // Show the table header and style it
            table.Style.ShowHeader <- true
            table.Style.HeaderStyle.BackgroundBrush <- PdfBrushes.Gray
            table.Style.HeaderStyle.TextBrush <- PdfBrushes.White
            table.Style.HeaderStyle.StringFormat <- new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle)
            table.Style.DefaultStyle.Font <- new PdfTrueTypeFont("¸¼Àº °íµñ", 9f, PdfFontStyle.Regular, true )

            // Set text alignment for the rest of the cells
            for i in 0 .. table.Columns.Count - 1 do
                table.Columns.[i].StringFormat <- new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle)

            // Draw the table on the page, starting below the title
            table.Draw(page, PointF(0f, tableStartY)) |> ignore

        // Save the PDF document to the specified file path
        doc.SaveToFile(pdfFilePath)

        // Indicate completion
        printfn "PDF document '%s' has been created successfully." pdfFilePath



