// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Runtime.CompilerServices
open DocumentFormat.OpenXml.Packaging
open System
open System.Linq
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Drawing
open System.IO
open Engine.Core
open System.Collections.Generic
open Dual.Common.Core.FS

[<AutoOpen>]
module PPTUtil =
    //open DocumentFormat.OpenXml.Presentation
    //open Presentation 사용금지 직접 네임스페이스 추가 혹은 type 정의 (Drawing 와 혼선)
    //ex) type GroupShape = DocumentFormat.OpenXml.Presentation.GroupShape
    type NonVisualDrawingProperties = Presentation.NonVisualDrawingProperties
    type ConnectionShape = Presentation.ConnectionShape
    type Shape = Presentation.Shape
    type Picture = Presentation.Picture
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

    let Objkey (iPage, Id) = $"{iPage}page{Id}"
    let TrimSpace (name: string) = name.TrimStart(' ').TrimEnd(' ')

    let CopyName (name: string, cnt) =
        sprintf "Copy%d_%s" cnt (name.Replace(".", "_"))

    let GetSysNFlow (fileName: string, name: string, pageNum: int) =
        if (name.StartsWith("$")) then
            if name.Contains(".") then
                (TrimSpace(name.Split('.').[0]).TrimStart('$')), TrimSpace(name.Split('.').[1])
            else
                (TrimSpace(name.TrimStart('$'))), "_"
        elif (name = "") then
            fileName, sprintf "Page%d" pageNum
        else
            fileName, TrimSpace(name)


            
    let GetAliasNumber (names: string seq) =
        let usedNames = HashSet<string>()

        seq {

            let Number (testName) =
                if names |> Seq.filter (fun name -> name = testName) |> Seq.length = 1 then
                    usedNames.Add(testName) |> ignore
                    0
                else
                    let mutable cnt = 0
                    let mutable copy = testName

                    while usedNames.Contains(copy) do
                        if (cnt > 0) then
                            copy <- CopyName(testName, cnt)

                        cnt <- cnt + 1

                    usedNames.Add(copy) |> ignore
                    cnt

            for name in names do
                yield name, Number(name) - 1
        }


    let getSlideNotes (slidePart: SlidePart option) (pathOrPage: string) (pageNum:int)=
        match slidePart with
        | Some page when page.NotesSlidePart <> null ->
            page.NotesSlidePart.NotesSlide.InnerText
                .Replace("&nbsp", "")
                .Replace("\u00A0", "")
                .TrimEnd(pageNum.ToString().ToCharArray()) // Remove the trailing '1' which is a page number
        | _ -> 
            ""


    [<Extension>]
    type Office =
        [<Extension>]
        static member ErrorName(shape: Shape, errMsg: string, page: int) =
            Office.ErrorPPT(
                ErrorCase.Name,
                errMsg,
                Office.ShapeName(shape),
                page,
                Office.ShapeID(shape),
                shape.InnerText
            )

        [<Extension>]
        static member ErrorPath(shape: Shape, errMsg: string, page: int, path: string) =
            Office.ErrorPPT(ErrorCase.Page, errMsg, Office.ShapeName(shape), page, Office.ShapeID(shape), path)

        [<Extension>]
        static member ErrorShape(shape: Shape, errMsg: string, page: int) =
            Office.ErrorPPT(
                ErrorCase.Shape,
                errMsg,
                Office.ShapeName(shape),
                page,
                Office.ShapeID(shape),
                shape.InnerText
            )

        [<Extension>]
        static member ErrorConnect(conn: #ConnectionShape, errMsg: string, text: string, page: int) =
            Office.ErrorPPT(ErrorCase.Conn, errMsg, $"{text}", page, Office.ConnectionShapeID(conn), conn.InnerText)

        [<Extension>]
        static member ErrorConnect(conn: #ConnectionShape, errMsg: string, src: string, tgt: string, page: int) =
            Office.ErrorConnect(conn, errMsg, $"{src}~{tgt}", page)

        ///power point 문서를 Openxml로 열기 (*.pptx 형식만 지원)
        [<Extension>]
        static member Open(path: string) = PresentationDocument.Open(path, false)

        //shape ID 구하기
        [<Extension>]
        static member GetId(shape: Shape) =
            shape
                .Descendants<NonVisualShapeProperties>().First()
                .Descendants<NonVisualDrawingProperties>().First()
                .Id

        [<Extension>]
        static member IsOutlineExist(shape: Shape) =
            let outline =
                shape
                    .Descendants<ShapeProperties>().First()
                    .Descendants<Drawing.Outline>()
                    .FirstOrDefault()

            if (outline = null) then
                shape.Descendants<ShapeStyle>().Any()
            else
                outline.Descendants<Drawing.NoFill>().Any() |> not

        [<Extension>]
        static member IsOutlineConnectionExist(shape: #ConnectionShape) =
            let outline =
                shape
                    .Descendants<ShapeProperties>().First()
                    .Descendants<Drawing.Outline>()
                    .FirstOrDefault()

            if (outline = null) then
                shape.Descendants<ShapeStyle>().Any()
            else
                outline.Descendants<Drawing.NoFill>().Any() |> not

        [<Extension>]
        static member getConnectionHeadTail(outline: #Drawing.Outline) =
            let head =
                let headEnd = outline.Descendants<HeadEnd>().FirstOrDefault()
                if (headEnd = null || headEnd.Type = null) then
                    LineEndValues.None
                else
                    outline.Descendants<HeadEnd>().FirstOrDefault().Type.Value

            let tail =
                let tailEnd = outline.Descendants<TailEnd>().FirstOrDefault()
                if (tailEnd = null || tailEnd.Type = null) then
                    LineEndValues.None
                else
                    outline.Descendants<TailEnd>().FirstOrDefault().Type.Value

            head, tail

        [<Extension>]
        static member IsNonDirectional(shape: #ConnectionShape) =
            let outline =
                shape
                    .Descendants<ShapeProperties>().First()
                    .Descendants<Drawing.Outline>().FirstOrDefault()

            if (outline = null) then
                true
            else
                let head, tail = outline.getConnectionHeadTail()

                head = LineEndValues.None && tail = LineEndValues.None

        /// shape 하부 속성 중에 outline 혹은 style 설정 되어 있고, geometry 가 있는지 여부 반환
        [<Extension>]
        static member CheckShape(shape: Shape) =
            let shapeProperties = shape.Descendants<ShapeProperties>().FirstOrDefault()

            shapeProperties <> null
            && (shapeProperties.Descendants<Drawing.Outline>().Any()
                || shape.Descendants<ShapeStyle>().Any())
            && (shapeProperties.Descendants<Drawing.Transform2D>().Any()
                || shapeProperties.Descendants<Drawing.PresetGeometry>().Any())

        [<Extension>]
        static member ShapeName(shape: Shape) =
            let shapeProperties = shape.Descendants<NonVisualShapeProperties>().First()
            let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().First()
            prop.Name.Value

        [<Extension>]
        static member IsUnderlined(shape: Shape) =
            shape.Descendants<TextBody>()
            |> Seq.collect (fun textBody -> textBody.Descendants<Paragraph>())
            |> Seq.collect (fun paragraph -> paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Run>())
            |> Seq.exists (fun run ->
                match run.RunProperties with
                | null -> false
                | runProps -> runProps.Underline <> null && runProps.Underline.InnerText = "sng" //DocumentFormat.OpenXml.Drawing.TextUnderlineValues.Single
            )

        [<Extension>]
        static member IsStrikethrough(shape: Shape) =
            shape.Descendants<TextBody>()
            |> Seq.collect (fun textBody -> textBody.Descendants<Paragraph>())
            |> Seq.collect (fun paragraph -> paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Run>())
            |> Seq.exists (fun run ->
                match run.RunProperties with
                | null -> false
                | runProps -> runProps.Strike <> null && runProps.Strike.InnerText = "sngStrike" //DocumentFormat.OpenXml.Drawing.TextStrikeValues.Single
            )
        

        [<Extension>]
        static member ShapeID(shape: Shape) =
            let shapeProperties = shape.Descendants<NonVisualShapeProperties>().First()

            let prop =
                shapeProperties.Descendants<NonVisualDrawingProperties>().First()

            prop.Id.Value

        [<Extension>]
        static member ConnectionShapeID(shape: ConnectionShape) =
            let shapeProperties =
                shape.Descendants<NonVisualConnectionShapeProperties>().First()

            let prop =
                shapeProperties.Descendants<NonVisualDrawingProperties>().First()

            prop.Id.Value

        [<Extension>]
        static member GroupName(gShape: #GroupShape) =
            let shapeProperties =
                gShape.Descendants<NonVisualGroupShapeProperties>().First()

            let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().First()
            prop.Name.Value

        //shape Position 구하기
        [<Extension>]
        static member GetPosition(shape: Shape, slideSize: int * int) =
            let transform2D =
                shape
                    .Descendants<ShapeProperties>()
                    .FirstOrDefault()
                    .Descendants<Drawing.Transform2D>()
                    .FirstOrDefault()

            let xy = transform2D.Descendants<Drawing.Offset>().FirstOrDefault() //좌상단 x,y
            let wh = transform2D.Descendants<Drawing.Extents>().FirstOrDefault()
            let cx, cy = slideSize
            let fullHDx = 1920f
            let fullHDy = 1080f
            let leftTopX = Convert.ToSingle(xy.X.Value) (*+(Convert.ToSingle(wh.Cx.Value)/2f)*)
            let leftTopY = Convert.ToSingle(xy.Y.Value) (*+(Convert.ToSingle(wh.Cy.Value)/2f)*)
            let x = leftTopX / Convert.ToSingle(cx) * fullHDx |> Convert.ToInt32
            let y = leftTopY / Convert.ToSingle(cy) * fullHDy |> Convert.ToInt32

            let w =
                Convert.ToSingle(wh.Cx.Value) / Convert.ToSingle(cx) * fullHDx
                |> Convert.ToInt32

            let h =
                Convert.ToSingle(wh.Cy.Value) / Convert.ToSingle(cy) * fullHDy
                |> Convert.ToInt32

            System.Drawing.Rectangle(x, y, w, h)

        [<Extension>]
        static member IsRound(geometry: #Drawing.PresetGeometry) =
            let shapeGuide =
                geometry
                    .Descendants<Drawing.AdjustValueList>().First()
                    .Descendants<Drawing.ShapeGuide>()

            shapeGuide.Any() |> not || shapeGuide.First().Formula.Value = "val 0" |> not
      
        [<Extension>]
        static member GetGeometry(shape: Shape) =
                    shape
                        .Descendants<ShapeProperties>().First()
                        .Descendants<Drawing.PresetGeometry>()
                        .FirstOrDefault()

        [<Extension>]
        static member GetShapeGuide(geometry: #Drawing.PresetGeometry) =
            //shapeGuide 없으면 기본 Round
            geometry.Descendants<Drawing.AdjustValueList>().First()
                    .Descendants<Drawing.ShapeGuide>()

        [<Extension>]
        static member IsBevelShapeRound(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry <> null && geometry.Preset.Value = Drawing.ShapeTypeValues.Bevel
                then 
                    let shapeGuide = geometry.GetShapeGuide()
                    if shapeGuide.Any() 
                    then 
                        let formulaVal = shapeGuide.First().Formula.Value 
                        formulaVal <> "val 0" && formulaVal <> "val 50000"
                    else 
                        true//shapeGuide 없으면 기본 ShapeRound
                     
                else false
               
                    
        [<Extension>]
        static member IsBevelShapeMaxRound(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry <> null && geometry.Preset.Value = Drawing.ShapeTypeValues.Bevel
                then 
                    let shapeGuide = geometry.GetShapeGuide()
                    if shapeGuide.Any() 
                    then 
                        let formulaVal = shapeGuide.First().Formula.Value 
                        formulaVal = "val 50000"
                    else 
                        false//shapeGuide 없으면 기본 ShapeRound
                     
                else false

        [<Extension>]
        static member IsBevelShapePlate(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry <> null && geometry.Preset.Value = Drawing.ShapeTypeValues.Bevel
                then 
                    let shapeGuide = geometry.GetShapeGuide()
                    if shapeGuide.Any() 
                    then 
                        let formulaVal = shapeGuide.First().Formula.Value 
                        formulaVal = "val 0"
                    else 
                        false//shapeGuide 없으면 기본 ShapeRound
                     
                else false

        [<Extension>]
        static member IsBevelShape(shape: Shape) =
                      shape.IsBevelShapePlate()
                      || shape.IsBevelShapeRound()
                      || shape.IsBevelShapeMaxRound()

        [<Extension>]
        static member CheckDonutShape(shape: Shape) =

            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()

                (geometry.Preset.Value = Drawing.ShapeTypeValues.Donut)



        [<Extension>]
        static member IsEllipse(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    let round =  geometry.IsRound() 


                    (geometry.Preset.Value = Drawing.ShapeTypeValues.Ellipse
                     || (geometry.Preset.Value = Drawing.ShapeTypeValues.RoundRectangle && round)
                     || geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartAlternateProcess
                     || geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartConnector)



        [<Extension>]
        static member IsRectangle(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    let round =  geometry.IsRound() 
                    (geometry.Preset.Value = Drawing.ShapeTypeValues.Rectangle
                        || geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartProcess
                        || (geometry.Preset.Value = Drawing.ShapeTypeValues.RoundRectangle && round|> not)
                        || (geometry.Preset.Value = Drawing.ShapeTypeValues.HomePlate && round|> not)
                        )

        [<Extension>] 
        static member IsFoldedCornerPlate(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    let round =  geometry.IsRound() 
                    (geometry.Preset.Value = Drawing.ShapeTypeValues.FoldedCorner && not <| round)

        [<Extension>]
        static member IsFoldedCornerRound(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    let round =  geometry.IsRound() 
                    (geometry.Preset.Value = Drawing.ShapeTypeValues.FoldedCorner && round)

        [<Extension>]
        static member IsHomePlate(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    let round =  geometry.IsRound() 

                    (geometry.Preset.Value = Drawing.ShapeTypeValues.HomePlate && round
                     || geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartOffpageConnector)

        [<Extension>]
        static member CheckNoSmoking(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    geometry.Preset.Value = Drawing.ShapeTypeValues.NoSmoking
        
        //[<Extension>]
        //static member CheckFlowChartDecision(shape: Shape) =
        //    if (Office.CheckShape(shape) |> not) then
        //        false
        //    else
        //        let geometry = shape.GetGeometry()
        //        geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartDecision

        [<Extension>]
        static member IsFlowChartPreparation(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    geometry.Preset.Value = Drawing.ShapeTypeValues.FlowChartPreparation

        [<Extension>]
        static member CheckBlockArc(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    geometry.Preset.Value = Drawing.ShapeTypeValues.BlockArc


        // Layout  정의 블록
        [<Extension>]
        static member IsLayout(shape: Shape) =
            if (Office.CheckShape(shape) |> not) then
                false
            else
                let geometry = shape.GetGeometry()
                if geometry = null 
                then false
                else 
                    geometry.Preset.Value = Drawing.ShapeTypeValues.Frame

        [<Extension>]
        static member IsDashShape(shape: Shape) =
            if
                (shape
                    .Descendants<ShapeProperties>().First()
                    .Descendants<Drawing.Outline>().Any()
                 |> not)
            then
                false
            else
                let presetDash =
                    shape
                        .Descendants<ShapeProperties>().First()
                        .Descendants<Drawing.Outline>().First()
                        .Descendants<Drawing.PresetDash>()

                presetDash.Any()
                && presetDash.First().Val.Value = Drawing.PresetLineDashValues.Solid
                   |> not

        [<Extension>]
        static member IsDashLine(conn: ConnectionShape) =
            let shapeProperties = conn.Descendants<ShapeProperties>().First()
            let outline = shapeProperties.Descendants<Drawing.Outline>().First()
            let presetDash = outline.Descendants<Drawing.PresetDash>().FirstOrDefault()

            (presetDash = null || presetDash.Val.Value = Drawing.PresetLineDashValues.Solid)
            |> not

        [<Extension>]
        static member EdgeName(conn: #ConnectionShape) =
            let shapeProperties =
                conn.Descendants<NonVisualConnectionShapeProperties>().First()

            let prop =
                shapeProperties.Descendants<NonVisualDrawingProperties>().First()

            prop.Name.Value

        [<Extension>]
        static member IsTitleBox(shape: Shape) =
            if (shape.Descendants<ApplicationNonVisualDrawingProperties>().Any() |> not) then
                false
            elif
                (shape
                    .Descendants<ApplicationNonVisualDrawingProperties>().First()
                    .Descendants<PlaceholderShape>()
                    .Any()
                 |> not)
            then
                false
            else
                true



        [<Extension>]
        static member IsSlideLayoutBlankType(slidePart: #SlidePart) =
            let slideLayoutType = slidePart.SlideLayoutPart.SlideLayout.Type

            if slideLayoutType = null then
                false
            else
                slideLayoutType.InnerText = "blank"


        [<Extension>]
        static member PageTitle(slidePart: #SlidePart) =
            let titleTexts =
                slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>()
                |> Seq.choose (fun shape -> 
                    let appNonVisProps = shape.Descendants<ApplicationNonVisualDrawingProperties>() |> Seq.tryHead
                    match appNonVisProps with
                    | Some xs -> 
                        let placeholderShape = xs.Descendants<PlaceholderShape>() |> Seq.tryHead
                        match placeholderShape with
                        | Some ps when ps.Type <> null && ps.Type.InnerText.ToLower().Contains("title") -> 
                            Some(shape.InnerText)
                        | _ -> None
                    | None -> None)

            if titleTexts |> Seq.isEmpty |> not then
                titleTexts |> Seq.head |> trimSpace |> trimNewLine
            else
                ""

        [<Extension>]
        static member GetPage(slidePart: SlidePart) =
            slidePart.Uri.OriginalString.Replace("/ppt/slides/slide", "").Split('.').[0]
            |> int

        ///슬라이드 모든 페이지를 반환(슬라이드 숨기기 속성 포함)
        [<Extension>]
        static member SlidesAll(doc: PresentationDocument) =
            doc.PresentationPart.SlideParts
            |> Seq.map (fun slidePart ->
                let show = slidePart.Slide.Show = null || slidePart.Slide.Show.InnerText = "1"
                let page = slidePart |> Office.GetPage
                slidePart, show, page)
            |> Seq.sortBy (fun (slidePart, show, page) -> page)

        [<Extension>]
        static member SlidesSkipHide(doc: PresentationDocument) =
            Office.SlidesAll(doc)
            |> Seq.filter (fun (slide, show, page) -> show)
            |> Seq.map (fun (slide, show, page) -> slide, page)


        ///전체 사용된 도형 반환 (Text box 제외)
        [<Extension>]
        static member IsValidShape(shape: Shape) =
            (shape.IsRectangle() //real
             || shape.IsEllipse() //call
             || shape.IsBevelShapeMaxRound() //condition
             || shape.IsBevelShapeRound() //btn
             || shape.IsBevelShapePlate() //lamp
             || shape.IsFoldedCornerRound() //COPY_DEV
             || shape.IsFoldedCornerPlate() //OPEN_EXSYS_LINK
             || shape.IsHomePlate() //interface
             || shape.IsFlowChartPreparation() //CallRX
             || shape.IsLayout())


      

       ///전체 사용된 에러 체크 반환 (Text box 제외)
        [<Extension>]
        static member CheckValidShapes(slidePart:SlidePart, page :int, ableShapes :Shape seq) =
            // Assume IsTextBox is a method that determines if a shape is a text box
            let isNonTextShape (shape: Shape) =
                not (shape.IsTitleBox()) && not (shape.Descendants<TextBody>().Any())

            let allShapes = slidePart.GetShapeTreeShapes() |> Seq.filter(fun f-> Office.IsValidShape(f))
            allShapes
                |> Seq.filter isNonTextShape
                |> Seq.except (ableShapes)
                |> Seq.iter   (fun f -> f.ErrorShape(ErrID._39, page))


        [<Extension>]
        static member SlideSize(doc: PresentationDocument) =
            let Cx = doc.PresentationPart.Presentation.SlideSize.Cx
            let Cy = doc.PresentationPart.Presentation.SlideSize.Cy
            Cx |> int, Cy |> int




        [<Extension>]
        static member GetTablesWithPageNumbers(doc: PresentationDocument, colCnt: int) =
            let tablesWithPageNumbers =
                Office.SlidesSkipHide(doc)
                |> Seq.map (fun (slidePart ,pageIndex)  ->
                    let dt = new System.Data.DataTable()

                    for i in 0 .. colCnt - 1 do
                        dt.Columns.Add($"col{i}") |> ignore

                    let gfs =
                        slidePart.Slide.CommonSlideData.ShapeTree.Descendants<DocumentFormat.OpenXml.Presentation.GraphicFrame>()

                    let tables =
                        gfs
                            .SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.Graphic>())
                            .SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.GraphicData>())
                            .SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.Table>())

                    let rows =
                        tables.SelectMany(fun s -> s.Descendants<DocumentFormat.OpenXml.Drawing.TableRow>())

                    rows
                    |> Seq.iter (fun row ->
                        let cells = row.Descendants<DocumentFormat.OpenXml.Drawing.TableCell>()
                        let rowTemp = dt.NewRow()
                        let cellTexts = cells |> Seq.map (fun cell ->
                                    cell.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>()
                                    |> Seq.filter(fun node -> node.InnerText <> "")
                                    |> Seq.map (fun node ->node.InnerText)
                                    |> String.concat "\r\n"
                                ) 
                                    
                        rowTemp.ItemArray <- cellTexts |> Seq.cast<obj> |> Seq.toArray 
                        rowTemp |> dt.Rows.Add |> ignore
                            )

                    pageIndex, dt)

            tablesWithPageNumbers |> Seq.where(fun (f,t) -> t.Rows.Count > 0)

        [<Extension>]
        static member GetLayoutPages(doc: PresentationDocument) =
            let getShapes (slidePart:SlidePart) = slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>()
            let layoutPages =
                Office.SlidesSkipHide(doc)
                |> Seq.filter (fun (slidePart ,_) -> getShapes(slidePart).Where(fun s->s.IsLayout()).Any())
            layoutPages


        [<Extension>]
        static member GetLayouts(doc: PresentationDocument) =

            let layoutList = HashSet<string>()
            let layouts =
                doc.GetLayoutPages()
                |> Seq.collect (fun (slidePart ,pageIndex) ->
                    let shapes = slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>()
                    let layouts = shapes.Where(fun f->f.ShapeName().StartsWith("TextBox") && f.InnerText.StartsWith("[Layout]"))
                    let paths = shapes.Where(fun f->f.ShapeName().StartsWith("TextBox") && f.InnerText.StartsWith("[Path]"))
                    if paths.Any() && layouts.Any()
                    then
                        let layout  = layouts.First().InnerText.Split(']').Last()
                        let path  = paths.First().InnerText.Split(']').Last()
                           
                        if layoutList.Contains layout
                        then 
                             Office.ErrorPPT(ErrorCase.Page, ErrID._66, "Duplicate layout names found", pageIndex, 0u)
                        else 
                            layoutList.Add  layout|>ignore

                        shapes.Where(fun s-> s.IsLayout())        
                              .Select(fun s-> layout, path, s.InnerText, s.GetPosition(doc.SlideSize()))
                    else
                        Office.ErrorPPT(ErrorCase.Page, ErrID._63, "Layouts page Error", pageIndex, 0u)
                  )
            layouts


        [<Extension>]
        static member SaveSlideImage((doc: PresentationDocument), (tempDirName:string)) =
            let extractSlideImage(slidePart: SlidePart, outputImagePath:string) =
                let imagePart = slidePart.GetPartsOfType<ImagePart>().FirstOrDefault()
                match imagePart with
                | null ->
                    Office.ErrorPPT(ErrorCase.Page, ErrID._65, "Layouts image Error", slidePart.GetPage(), 0u)
                | _ ->
                    use fileStream = new FileStream(outputImagePath, FileMode.Create)
                    imagePart.GetStream().CopyTo(fileStream)
            let imgs = HashSet<string>()
            doc.GetLayoutPages()
            |> Seq.iter (fun (slidePart ,pageIndex) ->
                let shapes = slidePart.GetShapeTreeShapes().ToArray()
                let layouts = shapes.Where(fun f->f.ShapeName().StartsWith("TextBox") && f.InnerText.StartsWith("[Layout]"))
                let paths = shapes.Where(fun f->f.ShapeName().StartsWith("TextBox") && f.InnerText.StartsWith("[Path]"))
                if paths.Any() && layouts.Any()
                then
                    let layout  = layouts.First().InnerText.Split(']').Last()
                    let path  = paths.First().InnerText.Split(']').Last()
                    if path = DsText.TextImageChannel 
                    then
                        let tempFileDir = Path.Combine(Path.GetTempPath(), tempDirName)
                        if not <| Directory.Exists(tempFileDir) then Directory.CreateDirectory(tempFileDir) |>ignore

                        let outputImagePath = Path.Combine(tempFileDir, sprintf "%s.jpg" layout)
                        imgs.Add outputImagePath|>ignore
                        extractSlideImage (slidePart, outputImagePath)
                        
                else
                    Office.ErrorPPT(ErrorCase.Page, ErrID._63, "Layouts page Error", pageIndex, 0u)
                )
            imgs


        [<Extension>]
        static member GetFirstSlideNote(path: string) =
            let doc = Office.Open(path)
            
            let firstSlidePart = 
                doc.PresentationPart.SlideParts 
                |> Seq.sortBy(fun f -> Office.GetPage f)
                |> Seq.tryHead

            getSlideNotes firstSlidePart $"path:{path}" 1


        [<Extension>]
        static member GetSlideNoteText(doc: PresentationDocument, iPage: int):string =
            let slideParts = 
                doc.PresentationPart.SlideParts 
                |> Seq.sortBy(fun f -> Office.GetPage f)
            
            let slidePart = 
                slideParts 
                |> Seq.skip (iPage - 1) 
                |> Seq.tryHead

            getSlideNotes slidePart $"iPage:{iPage}" iPage



            
        /// 전체 사용된 도형 반환 (Text box 제외)
        [<Extension>]
        static member GetShapeAndGeometries(shapes: Shape seq) : (Shape * ShapeTypeValues) seq =
                shapes
                |> Seq.filter (fun shape -> shape.IsValidShape())
                |> Seq.filter (fun f -> not(f.ShapeName().StartsWith("TextBox")))
                |> Seq.map (fun shape ->
                    let geometry =
                        shape.Descendants<Drawing.PresetGeometry>().First().Preset.Value

                    shape, geometry)   
                    
                    
        /// 마스터페이지 객체틀 종류에서 <#내용> 항목만 골라냄
        [<Extension>]
        static member GetPlaceholderShapes(shapes: Shape seq) : Shape seq =
                    shapes |> Seq.filter (fun shape -> shape.Descendants<PlaceholderShape>().Any())

        [<Extension>]
        static member private GetShapeTreeElements<'T when 'T :> OpenXmlElement>(slidePart: SlidePart) =
            let slideElements = slidePart.Slide.CommonSlideData.ShapeTree.Descendants<'T>()
            let layoutElements =
                match slidePart.SlideLayoutPart with
                | null -> Seq.empty
                | slideLayoutPart -> slideLayoutPart.SlideLayout.CommonSlideData.ShapeTree.Descendants<'T>()
            Seq.append slideElements layoutElements

        [<Extension>]
        static member GetShapeTreeShapes(slidePart: SlidePart) =
            Office.GetShapeTreeElements<Shape>(slidePart)

        [<Extension>]
        static member GetShapeTreeConnectionShapes(slidePart: SlidePart) =
            Office.GetShapeTreeElements<ConnectionShape>(slidePart)

        [<Extension>]
        static member GetShapeTreeGroupShapes(slidePart: SlidePart) =
            Office.GetShapeTreeElements<GroupShape>(slidePart)

            
        /// 마스터페이지 객체틀 종류에서 <#내용> 항목만 골라냄
        [<Extension>]
        static member PagePlaceHolderShapes(doc: PresentationDocument) =
            let getPlaceHolderId (shape:Shape) = shape.Descendants<ApplicationNonVisualDrawingProperties>().First().Descendants<PlaceholderShape>().First().Index
            Office.SlidesSkipHide(doc)
            |> Seq.collect (fun (slidePart,_) ->
                let page = slidePart |> Office.GetPage
                let masterPlaceHolders = Office.GetPlaceholderShapes(slidePart.SlideLayoutPart.SlideLayout.CommonSlideData.ShapeTree.Descendants<Shape>())
                                               .Where(fun f -> f.InnerText.TrimStart().StartsWith("<#") && f.InnerText.TrimEnd().EndsWith(">"))

                let errShapes = masterPlaceHolders.Where(fun s->s.InnerText.Split('#').Count() <> 2)
                if errShapes.any()
                then 
                    failWithLog $"{errShapes.First().InnerText} MasterPage PlaceHolder Error"

                Office.GetPlaceholderShapes(slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>())
                |> Seq.choose(fun shape -> 
                    match masterPlaceHolders.TryFind(fun f->getPlaceHolderId(f) = getPlaceHolderId(shape)) with
                    | Some mp -> Some(mp.InnerText.Trim(), shape.InnerText.Trim(), page)
                    | None -> None 
                    )
                )
                    
            |> Seq.toArray


        ///전체 사용된 도형 반환 (Text box 제외)
        [<Extension>]
        static member PageShapesNotUsingMasterPage(doc: PresentationDocument) =
            Office.SlidesSkipHide(doc)
            |> Seq.collect (fun (slidePart,_) ->
                let page = slidePart |> Office.GetPage
                Office.GetShapeAndGeometries(slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>())
                |> map(fun (shape, geometry) -> shape, page, geometry))

        ///전체 사용된 도형 반환 (Text box 제외)
        [<Extension>]
        static member PageShapes(doc: PresentationDocument) =
            Office.SlidesSkipHide(doc)
            |> Seq.collect (fun (slidePart,_) ->
                let page = slidePart |> Office.GetPage
                Office.GetShapeAndGeometries(slidePart.GetShapeTreeShapes())
                |> map(fun (shape, geometry) -> shape, page, geometry))

    