// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Runtime.CompilerServices
open DocumentFormat.OpenXml.Packaging
open System
open System.Linq
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Drawing
open System.Data
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Spreadsheet
open System.Data

[<AutoOpen>]
module PPTUtil =
    //open DocumentFormat.OpenXml.Presentation
    //open Presentation 사용금지 직접 네임스페이스 추가 혹은 type 정의 (Drawing 와 혼선)
    //ex) type GroupShape = DocumentFormat.OpenXml.Presentation.GroupShape
    type NonVisualDrawingProperties = Presentation.NonVisualDrawingProperties
    type ConnectionShape = Presentation.ConnectionShape
    type Shape = Presentation.Shape
    type NonVisualShapeProperties = Presentation.NonVisualShapeProperties
    type NonVisualGroupShapeProperties = Presentation.NonVisualGroupShapeProperties
    type NonVisualGroupShapeDrawingProperties = Presentation.NonVisualGroupShapeDrawingProperties

    type NonVisualConnectionShapeProperties = Presentation.NonVisualConnectionShapeProperties
    type NonVisualShapeDrawingProperties = Presentation.NonVisualShapeDrawingProperties
    type ApplicationNonVisualDrawingProperties = Presentation.ApplicationNonVisualDrawingProperties
    type PlaceholderShape = Presentation.PlaceholderShape
    type CommonSlideData = Presentation.CommonSlideData

    type ShapeProperties = Presentation.ShapeProperties
    type ShapeStyle = Presentation.ShapeStyle
    type SlideId = Presentation.SlideId
    type GroupShape = Presentation.GroupShape
    type TextBody = Presentation.TextBody
    
    

    [<Extension>]
    type Office =
        [<Extension>]
        static member ErrorName(shape:Shape, errMsg:string,  page:int) =
               Office.ErrorPPT(ErrorCase.Name, errMsg, Office.ShapeName(shape), page, Office.ShapeID(shape)  , shape.InnerText)

        [<Extension>]
        static member ErrorPath(shape:Shape, errMsg:string,  page:int, path:string) =
               Office.ErrorPPT(ErrorCase.Page, errMsg, Office.ShapeName(shape), page, Office.ShapeID(shape)  , path)

        [<Extension>]
        static member ErrorShape(shape:Shape, errMsg:string,  page:int) =
               Office.ErrorPPT(ErrorCase.Shape, errMsg, Office.ShapeName(shape), page, Office.ShapeID(shape)  ,shape.InnerText)

        [<Extension>]
        static member ErrorConnect(conn:#ConnectionShape, errMsg:string, text:string,  page:int) =
               Office.ErrorPPT(ErrorCase.Conn, errMsg, $"{text}", page, Office.ConnectionShapeID(conn), conn.InnerText)

        [<Extension>]
        static member ErrorConnect(conn:#ConnectionShape, errMsg:string, src:string, tgt:string,  page:int) =
               Office.ErrorConnect(conn, errMsg, $"{src}~{tgt}", page)

        ///power point 문서를 Openxml로 열기 (*.pptx 형식만 지원)
        [<Extension>]
        static member Open(path:string) = PresentationDocument.Open(path, false);

        //shape ID 구하기
        [<Extension>]
        static member GetId(shape:Shape) =
                         shape.Descendants<NonVisualShapeProperties>().First()
                              .Descendants<NonVisualDrawingProperties>().First().Id


        [<Extension>]
        static member IsOutlineExist(shape:Shape) =
            let outline = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.Outline>().FirstOrDefault();
            if(outline = null)
            then
                 shape.Descendants<ShapeStyle>().Any()
            else
                 outline.Descendants<Drawing.NoFill>().Any()|>not

        [<Extension>]
        static member IsOutlineConnectionExist(shape:#ConnectionShape) =
            let outline = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.Outline>().FirstOrDefault();
            if(outline = null)
            then
                 shape.Descendants<ShapeStyle>().Any()
            else
                 outline.Descendants<Drawing.NoFill>().Any()|>not

        [<Extension>]
        static member IsNonDirectional(shape:#ConnectionShape) =
            let outline = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.Outline>().FirstOrDefault();
            if(outline = null) then true
            else
                let head  = if(outline.Descendants<HeadEnd>().FirstOrDefault() = null)
                            then LineEndValues.None
                            else outline.Descendants<HeadEnd>().FirstOrDefault().Type.Value

                let tail  = if(outline.Descendants<TailEnd>().FirstOrDefault() = null)
                            then LineEndValues.None
                            else outline.Descendants<TailEnd>().FirstOrDefault().Type.Value

                head = LineEndValues.None && tail = LineEndValues.None

        [<Extension>]
        static member CheckShape(shape:Shape) =
            //도형이 아니면 필터  NonVisualShapeDrawingProperties
            let outline = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.Outline>().FirstOrDefault();
            if(outline = null && shape.Descendants<ShapeStyle>().Any()|>not) then false
            else
                if(shape.Descendants<ShapeProperties>().Any() |> not) then false
                else if(shape.Descendants<ShapeProperties>().FirstOrDefault().Descendants<Drawing.Transform2D>().Any() |> not) then false
                else if(shape.Descendants<ShapeProperties>().FirstOrDefault().Descendants<Drawing.PresetGeometry>().Any() |> not) then false
                else true

        [<Extension>]
        static member ShapeName(shape:Shape) =
                        let shapeProperties = shape.Descendants<NonVisualShapeProperties>().FirstOrDefault();
                        let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().FirstOrDefault();
                        prop.Name.Value

        [<Extension>]
        static member IsUnderlined(shape: Shape) =
                        shape.Descendants<TextBody>()
                        |> Seq.collect (fun textBody -> textBody.Descendants<Paragraph>())
                        |> Seq.collect (fun paragraph -> paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Run>())
                        |> Seq.exists (fun run ->
                            match run.RunProperties with
                            | null -> false
                            | runProps -> runProps.Underline <> null && runProps.Underline.InnerText = "sng"  //DocumentFormat.OpenXml.Drawing.TextUnderlineValues.Single
                        )

        [<Extension>]
        static member ShapeID(shape:Shape) =
                        let shapeProperties = shape.Descendants<NonVisualShapeProperties>().FirstOrDefault();
                        let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().FirstOrDefault();
                        prop.Id.Value
        [<Extension>]
        static member ConnectionShapeID(shape:ConnectionShape) =
                        let shapeProperties = shape.Descendants<NonVisualConnectionShapeProperties>().FirstOrDefault();
                        let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().FirstOrDefault();
                        prop.Id.Value

        [<Extension>]
        static member GroupName(gShape:#GroupShape) =
                        let shapeProperties = gShape.Descendants<NonVisualGroupShapeProperties>().FirstOrDefault();
                        let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().First();
                        prop.Name.Value

        //shape Position 구하기
        [<Extension>]
        static member GetPosition(shape:Shape, slideSize:int*int) =
                let transform2D = shape.Descendants<ShapeProperties>().FirstOrDefault().Descendants<Drawing.Transform2D>().FirstOrDefault()
                let xy = transform2D.Descendants<Drawing.Offset>().FirstOrDefault()  //좌상단 x,y
                let wh = transform2D.Descendants<Drawing.Extents>().FirstOrDefault()
                let cx, cy  = slideSize
                let fullHDx = 1920f
                let fullHDy = 1080f
                let leftTopX = Convert.ToSingle(xy.X.Value)(*+(Convert.ToSingle(wh.Cx.Value)/2f)*)
                let leftTopY = Convert.ToSingle(xy.Y.Value)(*+(Convert.ToSingle(wh.Cy.Value)/2f)*)
                let x = leftTopX/Convert.ToSingle(cx)*fullHDx |> Convert.ToInt32
                let y = leftTopY/Convert.ToSingle(cy)*fullHDy |> Convert.ToInt32
                let w = Convert.ToSingle(wh.Cx.Value)/Convert.ToSingle(cx)*fullHDx|> Convert.ToInt32
                let h = Convert.ToSingle(wh.Cy.Value)/Convert.ToSingle(cy)*fullHDy|> Convert.ToInt32
                System.Drawing.Rectangle(x,y,w,h)

        [<Extension>]
        static member CheckRound(geometry:#Drawing.PresetGeometry) =
                         let shapeGuide = geometry.Descendants<Drawing.AdjustValueList>().First().Descendants<Drawing.ShapeGuide>()
                         shapeGuide.Any()|>not || shapeGuide.First().Formula.Value = "val 0" |> not

        [<Extension>]
        static member CheckBevelShapeRound(shape:Shape) =
            if(Office.CheckShape(shape) |> not) then false
            else
                let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                let round =  geometry.CheckRound()
                geometry.Preset.Value = Drawing.ShapeTypeValues.Bevel && round

        [<Extension>]
        static member CheckBevelShapePlate(shape:Shape) =
            if(Office.CheckShape(shape) |> not) then false
            else
                let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                let notRound =  geometry.CheckRound() |> not
                geometry.Preset.Value = Drawing.ShapeTypeValues.Bevel && notRound

        [<Extension>]
        static member CheckDonutShape(shape:Shape) =

            if(Office.CheckShape(shape) |> not) then false
            else
                let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                (  geometry.Preset.Value = Drawing.ShapeTypeValues.Donut
                )



        [<Extension>]
        static member CheckEllipse(shape:Shape) =
            if(Office.CheckShape(shape) |> not) then false
            else
                let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                let round =  geometry.CheckRound()
                (  geometry.Preset.Value = Drawing.ShapeTypeValues.Ellipse
               || (geometry.Preset.Value = Drawing.ShapeTypeValues.RoundRectangle && round)
                || geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartAlternateProcess
                || geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartConnector)



        [<Extension>]
        static member CheckRectangle(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    let round =  geometry.CheckRound()
                    (   geometry.Preset.Value = Drawing.ShapeTypeValues.Rectangle
                    || (geometry.Preset.Value = Drawing.ShapeTypeValues.RoundRectangle && round|>not)
                    || (geometry.Preset.Value = Drawing.ShapeTypeValues.HomePlate && round|>not)
                    ||  geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartProcess)

        [<Extension>]
        static member CheckFoldedCornerPlate(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    let round =  geometry.CheckRound()
                    (   geometry.Preset.Value = Drawing.ShapeTypeValues.FoldedCorner && not <| round )

        [<Extension>]
        static member CheckFoldedCornerRound(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    let round =  geometry.CheckRound()
                    (   geometry.Preset.Value = Drawing.ShapeTypeValues.FoldedCorner && round )

        [<Extension>]
        static member CheckHomePlate(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    let round =  geometry.CheckRound()
                    (
                      geometry.Preset.Value = Drawing.ShapeTypeValues.HomePlate && round
                    ||geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartOffpageConnector)

        [<Extension>]
        static member CheckNoSmoking(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    geometry.Preset.Value = Drawing.ShapeTypeValues.NoSmoking

        [<Extension>]
        static member CheckBlockArc(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    geometry.Preset.Value = Drawing.ShapeTypeValues.BlockArc



        //system Condition  정의 블록
        [<Extension>]
        static member CheckCondition(shape:Shape) =
                if(Office.CheckShape(shape) |> not) then false
                 else
                    let geometry = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.PresetGeometry>().FirstOrDefault()
                    geometry.Preset.Value = Drawing.ShapeTypeValues.Frame

        [<Extension>]
        static member IsDashShape(shape:Shape) =
            if(shape.Descendants<ShapeProperties>().First().Descendants<Drawing.Outline>().Any()|>not) then false
            else
                let presetDash = shape.Descendants<ShapeProperties>().First().Descendants<Drawing.Outline>().FirstOrDefault().Descendants<Drawing.PresetDash>()
                presetDash.Any() && presetDash.FirstOrDefault().Val.Value = Drawing.PresetLineDashValues.Solid |>not

        [<Extension>]
        static member IsDashLine(conn:ConnectionShape) =
            let shapeProperties = conn.Descendants<ShapeProperties>().FirstOrDefault();
            let outline = shapeProperties.Descendants<Drawing.Outline>().FirstOrDefault();
            let presetDash = outline.Descendants<Drawing.PresetDash>().FirstOrDefault();
            (presetDash = null || presetDash.Val.Value = Drawing.PresetLineDashValues.Solid)
            |> not

        [<Extension>]
        static member EdgeName(conn:#ConnectionShape) =
                let shapeProperties = conn.Descendants<NonVisualConnectionShapeProperties>().FirstOrDefault();
                let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().FirstOrDefault();
                prop.Name.Value

        [<Extension>]
        static member IsTitleBox(shape:Shape) =
                    if (shape.Descendants<ApplicationNonVisualDrawingProperties>().Any() |> not) then false
                    elif (shape.Descendants<ApplicationNonVisualDrawingProperties>().First().Descendants<PlaceholderShape>().Any() |> not ) then false
                    else true

        
                        
        [<Extension>]
        static member IsSlideLayoutBlanckType(slidePart:#SlidePart) =
                    let slideLayoutType = slidePart.SlideLayoutPart.SlideLayout.Type
                    if slideLayoutType = null then true
                    else slideLayoutType.InnerText  = "blank"
                

        [<Extension>]
        static member PageTitle(slidePart:#SlidePart, headTitle:bool) =
                let tilteTexts =
                    slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>()
                        |> Seq.filter(fun shape -> shape.Descendants<ApplicationNonVisualDrawingProperties>().Any())
                        |> Seq.map(fun shape -> shape, shape.Descendants<ApplicationNonVisualDrawingProperties>().First())
                        |> Seq.filter(fun (shape, tilte) -> tilte.Descendants<PlaceholderShape>().Any())
                        |> Seq.filter(fun (shape, tilte) -> tilte.Descendants<PlaceholderShape>().First().Type <> null)
                        |> Seq.filter(fun (shape, tilte) -> 
                                                    (not(headTitle) && tilte.Descendants<PlaceholderShape>().First().Type.Value = Presentation.PlaceholderValues.Title)
                                                    ||  (headTitle  && tilte.Descendants<PlaceholderShape>().First().Type.Value = Presentation.PlaceholderValues.CenteredTitle)
                                )
                        |> Seq.map(fun (shape, tilte) -> shape.InnerText)

                if(tilteTexts.Any())
                then tilteTexts |>Seq.head |> trimSpace |> trimNewLine
                else ""

        [<Extension>]
        static member GetPage(slidePart:SlidePart) =
            slidePart.Uri.OriginalString.Replace("/ppt/slides/slide","").Split('.').[0] |> int

        ///슬라이드 모든 페이지를 반환(슬라이드 숨기기 속성 포함)
        [<Extension>]
        static member SlidesAll(doc:PresentationDocument) =
                        doc.PresentationPart.SlideParts
                        |> Seq.map (fun slidePart ->
                                let show = slidePart.Slide.Show = null || slidePart.Slide.Show.InnerText = "1"
                                let page = slidePart |> Office.GetPage
                                slidePart, show, page)
                        |> Seq.sortBy (fun (slidePart, show, page) -> page)

        ///슬라이드 Master 페이지를 반환
        [<Extension>]
        static member SlidesMasterAll(doc:PresentationDocument) =
                        doc.PresentationPart.SlideMasterParts
                        |> Seq.collect (fun slideMasterPart ->
                                slideMasterPart.SlideLayoutParts |> Seq.map(fun slidePart -> slidePart.SlideMasterPart.SlideMaster))


        [<Extension>]
        static member SlidesSkipHide(doc:PresentationDocument) =
                        Office.SlidesAll(doc)
                        |> Seq.filter(fun (slide, show, page) -> show)
                        |> Seq.map (fun (slide, show, page) ->  slide)


        ///전체 사용된 도형 반환 (Text box 제외)
        [<Extension>]
        static member IsAbleShape(shape:Shape) =
                    if (shape.CheckRectangle()      //real
                    || shape.CheckEllipse()         //call
                    || shape.CheckBevelShapeRound()      //btn
                    || shape.CheckBevelShapePlate()      //lamp
                    || shape.CheckFoldedCornerRound()    //COPY_DEV
                    || shape.CheckFoldedCornerPlate()    //OPEN_EXSYS_LINK
                    || shape.CheckHomePlate()      //interface
                    || shape.CheckCondition())       // system condition
                    then true
                    else false

        [<Extension>]
        static member Shapes(page:int, commonSlideData:CommonSlideData) =
                        let shapes = commonSlideData.ShapeTree.Descendants<Shape>()
                        let ableShapes =
                            shapes
                            |> Seq.filter(fun  shape -> shape.IsAbleShape())
                            |> Seq.map(fun  shape ->
                                    let geometry = shape.Descendants<Drawing.PresetGeometry>().FirstOrDefault().Preset.Value
                                    shape, page, geometry, shape.IsDashShape())

                        shapes
                        |> Seq.except (ableShapes |> Seq.map (fun (shape, page, geometry, isDash) -> shape))
                        |> Seq.filter(fun f -> f.IsTitleBox()|>not)
                        |> Seq.filter(fun f -> f.ShapeName().StartsWith("TextBox")|>not)
                        |> Seq.iter(fun f -> f.ErrorShape(ErrID._39, page))

                        ableShapes


        ///전체 사용된 도형 반환 (Text box 제외)
        [<Extension>]
        static member PageShapes(doc:PresentationDocument) =
                        Office.SlidesSkipHide(doc)
                        |> Seq.collect (fun slidePart ->
                                let page = slidePart |> Office.GetPage
                                Office.Shapes (page, slidePart.Slide.CommonSlideData))


        [<Extension>]
        static member SlideSize(doc:PresentationDocument) =
                        let Cx = doc.PresentationPart.Presentation.SlideSize.Cx
                        let Cy = doc.PresentationPart.Presentation.SlideSize.Cy
                        Cx |> int, Cy |> int


        [<Extension>]
        static member ExportDataTableToExcel (dataTables: DataTable seq) (filePath: string) =
            // Create a new spreadsheet document
            use spreadsheetDocument = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook)

            // Create the workbook
            let workbookPart = spreadsheetDocument.AddWorkbookPart()
            let workbook = new Workbook()

            // Create sheets collection
            let sheets = new Sheets()

            for (index, dataTable) in Seq.indexed dataTables do
                // Create a worksheet for each DataTable
                let worksheetPart = workbookPart.AddNewPart<WorksheetPart>()
                let worksheet = new Worksheet()

                // Create the sheet data
                let sheetData = new SheetData()

                // Add column headers to the sheet data
                let headerRow = new Row()
                for colIndex in 0 .. dataTable.Columns.Count - 1 do
                    let cell = new Cell()
                    cell.DataType <- CellValues.String
                    cell.CellValue <- new CellValue(dataTable.Columns.[colIndex].ColumnName)
                    headerRow.AppendChild(cell)|>ignore

                sheetData.AppendChild(headerRow)|>ignore

                // Populate the sheet data with data from the DataTable
                for rowIndex in 0 .. dataTable.Rows.Count - 1 do
                    let dataRow = new Row()
                    for colIndex in 0 .. dataTable.Columns.Count - 1 do
                        let cell = new Cell()
                        cell.DataType <- CellValues.String
                        cell.CellValue <- new CellValue(dataTable.Rows.[rowIndex].[colIndex].ToString())
                        dataRow.AppendChild(cell)|>ignore

                    sheetData.AppendChild(dataRow)|>ignore

                worksheet.AppendChild(sheetData)|>ignore

                worksheetPart.Worksheet <- worksheet

                // Create a sheet with a unique name
                let sheet = new Sheet(Name =  $"Sheet{(index + 1)}", Id = workbookPart.GetIdOfPart(worksheetPart))
                sheets.AppendChild(sheet)|>ignore

            workbook.AppendChild(sheets)|>ignore

            // Save the workbook
            workbookPart.Workbook <- workbook
            spreadsheetDocument.Save()




        [<Extension>]
        static member GetTablesWithPageNumbers(doc: PresentationDocument, colCnt: int) =
            let tablesWithPageNumbers =
                Office.SlidesSkipHide(doc)
                |> Seq.mapi (fun pageIndex slidePart ->
                    let dt = new System.Data.DataTable()
                    for i in 0 .. colCnt - 1 do
                        dt.Columns.Add($"col{i}") |> ignore

                    let gfs = slidePart.Slide.CommonSlideData.ShapeTree.Descendants<DocumentFormat.OpenXml.Presentation.GraphicFrame>()
                    let tables = gfs.SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.Graphic>())
                                    .SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.GraphicData>())
                                    .SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.Table>())

                    let rows = tables.SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.TableRow>())
                    rows
                    |> Seq.iter (fun row ->
                        let cells = row.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>()
                        if cells.Count() = colCnt then
                            let rowTemp = dt.NewRow()
                            rowTemp.ItemArray <- cells.Select(fun c -> c.InnerText) |> Seq.cast<obj> |> Seq.toArray
                            rowTemp |> dt.Rows.Add |> ignore
                        )
                    pageIndex+1, dt
                )
            tablesWithPageNumbers


     

