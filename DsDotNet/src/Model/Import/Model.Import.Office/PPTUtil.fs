// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Runtime.CompilerServices
open DocumentFormat.OpenXml.Packaging
open System
open System.Linq
open DocumentFormat.OpenXml
open DocumentFormat.OpenXml.Drawing

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
    
    [<Extension>] 
    type Office =

        [<Extension>] 
        static member ErrorName(shape:Shape, errMsg:string,  page:int) = 
               Office.ErrorPPT(ErrorCase.Name, errMsg, Office.ShapeName(shape), page, shape.InnerText)
        
        [<Extension>] 
        static member ErrorPath(shape:Shape, errMsg:string,  page:int, path:string) = 
               Office.ErrorPPT(ErrorCase.Page, errMsg, Office.ShapeName(shape), page, path)
        
        [<Extension>] 
        static member ErrorShape(shape:Shape, errMsg:string,  page:int) = 
               Office.ErrorPPT(ErrorCase.Shape, errMsg, Office.ShapeName(shape), page, shape.InnerText)

        [<Extension>] 
        static member ErrorConnect(conn:#ConnectionShape, errMsg:string, text:string,  page:int) = 
               Office.ErrorPPT(ErrorCase.Conn, errMsg, $"{Office.EdgeName(conn)}[{text}]", page, conn.InnerText)

        [<Extension>] 
        static member ErrorConnect(conn:#ConnectionShape, errMsg:string, src:string, tgt:string,  page:int) = 
               Office.ErrorConnect(conn, errMsg, $"{Office.EdgeName(conn)}[{src}~{tgt}]", page)

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
        static member GroupName(gShape:#GroupShape) = 
                        let shapeProperties = gShape.Descendants<NonVisualGroupShapeProperties>().FirstOrDefault();
                        let prop = shapeProperties.Descendants<NonVisualDrawingProperties>().First();
                        prop.Name.Value
    
        //shape Position 구하기
        [<Extension>] 
        static member GetPosition(shape:Shape, sildeSize:int*int) = 
                let transform2D = shape.Descendants<ShapeProperties>().FirstOrDefault().Descendants<Drawing.Transform2D>().FirstOrDefault()
                let xy = transform2D.Descendants<Drawing.Offset>().FirstOrDefault()  //좌상단 x,y
                let wh = transform2D.Descendants<Drawing.Extents>().FirstOrDefault()
                let cx, cy  = sildeSize
                let fullHDx = 1920f
                let fullHDy = 1080f
                let centerX = Convert.ToSingle(xy.X.Value)+(Convert.ToSingle(wh.Cx.Value)/2f)
                let centerY = Convert.ToSingle(xy.Y.Value)+(Convert.ToSingle(wh.Cy.Value)/2f)
                let x = centerX/Convert.ToSingle(cx)*fullHDx |> Convert.ToInt32
                let y = centerY/Convert.ToSingle(cy)*fullHDy |> Convert.ToInt32
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
        static member PageTitle(slidePart:#SlidePart) = 
                let tilteTexts = 
                    slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Shape>()
                        |> Seq.filter(fun shape -> shape.Descendants<ApplicationNonVisualDrawingProperties>().Any())
                        |> Seq.map(fun shape -> shape, shape.Descendants<ApplicationNonVisualDrawingProperties>().First())
                        |> Seq.filter(fun (shape, tilte) -> tilte.Descendants<PlaceholderShape>().Any())
                        |> Seq.filter(fun (shape, tilte) -> tilte.Descendants<PlaceholderShape>().First().Type.Value = Presentation.PlaceholderValues.Title)
                        |> Seq.map(fun (shape, tilte) -> shape.InnerText)
            
                if(tilteTexts.Any()) 
                then tilteTexts |>Seq.head
                else ""

        [<Extension>] 
        static member GetPage(slidePart:SlidePart) = 
            slidePart.Uri.OriginalString.Replace("/ppt/slides/slide","").Split('.').[0] |> int

        ///슬라이드 모든 페이지를 반환(슬라이드 숨기기 속성 포함)
        [<Extension>] 
        static member SildesAll(doc:PresentationDocument) = 
                        doc.PresentationPart.SlideParts
                        |> Seq.map (fun slidePart -> 
                                let show = slidePart.Slide.Show = null || slidePart.Slide.Show.InnerText = "1"
                                let page = slidePart |> Office.GetPage
                                slidePart, show, page)
                        |> Seq.sortBy (fun (slidePart, show, page) -> page)

        ///슬라이드 Master 페이지를 반환
        [<Extension>] 
        static member SildesMasterAll(doc:PresentationDocument) = 
                        doc.PresentationPart.SlideMasterParts
                        |> Seq.collect (fun slideMasterPart -> 
                                slideMasterPart.SlideLayoutParts |> Seq.map(fun slidePart -> slidePart.SlideMasterPart.SlideMaster))


        [<Extension>] 
        static member SildesSkipHide(doc:PresentationDocument) = 
                        Office.SildesAll(doc) 
                        |> Seq.filter(fun (slide, show, page) -> show)
                        |> Seq.map (fun (slide, show, page) ->  slide)
       
        ///전체 사용된 도형 반환 (Text box 제외)
        [<Extension>] 
        static member IsAbleShape(shape:Shape) = 
                    if (shape.CheckRectangle()      //real
                    || shape.CheckEllipse()         //call
                    || shape.CheckBevelShapeRound()      //btn
                    || shape.CheckBevelShapePlate()      //lamp
                    || shape.CheckFoldedCornerRound()    //copy_value
                    || shape.CheckFoldedCornerPlate()    //copy_ref
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
                        Office.SildesSkipHide(doc) 
                        |> Seq.collect (fun slidePart ->  
                                let page = slidePart |> Office.GetPage
                                Office.Shapes (page, slidePart.Slide.CommonSlideData))
                           
    
        [<Extension>] 
        static member SildeSize(doc:PresentationDocument) = 
                        let Cx = doc.PresentationPart.Presentation.SlideSize.Cx
                        let Cy = doc.PresentationPart.Presentation.SlideSize.Cy
                        Cx |> int, Cy |> int
        
    