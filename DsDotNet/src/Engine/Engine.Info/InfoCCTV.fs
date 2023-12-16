namespace Engine.Info.Func

open Emgu.CV
open Emgu.CV.CvEnum
open Emgu.CV.Structure
open Emgu.CV.Util
open Engine.Core
open System
open System.Collections.Generic
open System.Drawing
open System.Threading.Tasks
open System.Drawing.Text

[<AutoOpen>]
module dsCCTVM =
    type dsCCTV ()=
    
        let mutable FrontFrame = new Mat()
        let mutable BackFrame = new Mat()
        let _Width = 1920
        let _Height = 1080
        let ImageCenter = Point(_Width / 2, _Height / 2)
        let Offset = 10
        let SizeFont = 13.0f
        let mutable oflt, ofrt, oflb, ofrb, lt, rt, lb, rb, dc, pc  = Point(), Point(), Point(), Point(), Point(), Point(), Point(), Point(), Point(), Point()
        let mutable vTc, vTp = Point(),Point()
        let rectUpper = MCvScalar(0.0, 0.0, 0.0, 90.0)
        let rectSide = MCvScalar(0.0, 0.0, 0.0, 185.0)
        let rectLower = MCvScalar(0.0, 0.0, 0.0, 230.0)
        let textFont = new Font("Arial", SizeFont)
        let rowNames = ["당일 가동 횟수"; "당일 고장 횟수"; "평균 무고장 시간"]
        let errorHead = "에러 사유"

        let ResizeImage (img : Mat) (newWidth : int) (newHeight : int) (interpolationType : Inter) =
            let resizedImage = new Mat()
            CvInvoke.Resize(img, resizedImage, new Size(newWidth, newHeight), interpolation = interpolationType)
            resizedImage

        let CalcDistance (p1 : Point) (p2 : Point) =
            Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y)|> float)  |>int

        let TextPainter (text : string) (location : PointF) (color : Color) =
            let bitmap = FrontFrame.ToBitmap()
            use graphics = Graphics.FromImage(bitmap)
            let brush = new SolidBrush(color)
            graphics.TextRenderingHint <- TextRenderingHint.AntiAlias
            graphics.DrawString(text, textFont, brush, location) |> ignore
            let updatedImage = bitmap.ToImage<Bgra, byte>()
            FrontFrame <- updatedImage.Mat

        let GetNonZeroAlphaPixels (image : Mat) =
            if image.NumberOfChannels <> 4 then
                raise <| new System.ArgumentException("The image must have 4 channels (BGRA).")
    
            let alphaChannel = new Mat()
            let nonZeroCoordinates = new VectorOfPoint()
            let result = new List<Tuple<PointF, Bgra>>()

            CvInvoke.ExtractChannel(image, alphaChannel, 3)
            CvInvoke.FindNonZero(alphaChannel, nonZeroCoordinates)
            let imgBgra = image.ToImage<Bgra, byte>()

            for i = 0 to nonZeroCoordinates.Size - 1 do
                let point = nonZeroCoordinates.[i]
                let color = imgBgra.[point.Y, point.X]
                let tuple = new Tuple<PointF, Bgra>(new PointF(float32 point.X, float32 point.Y), color)
                result.Add(tuple)

            result

        let AlphaBlend (src : Mat) (dst : Mat) =
            if src.Size <> dst.Size then
                raise <| new System.ArgumentException("Both source and destination Mats should have the same size.")

            let dstImage = dst.ToImage<Bgra, byte>()
            let nonZeroCoordinates = GetNonZeroAlphaPixels src

            Parallel.ForEach(nonZeroCoordinates, fun point ->
                let (coord: PointF) = fst point
                let (srcColor: Bgra) = snd point
                let dstColor = dstImage.[int coord.Y, int coord.X]
                let alpha = srcColor.Alpha / 255.0
                let invAlpha = 1.0 - alpha
                dstImage.[int coord.Y, int coord.X] <- 
                    new Bgra(Blue = srcColor.Blue * alpha + dstColor.Blue * invAlpha,
                             Green = srcColor.Green * alpha + dstColor.Green * invAlpha,
                             Red = srcColor.Red * alpha + dstColor.Red * invAlpha,
                             Alpha = 255.0)
            ) |> ignore

            dstImage.Mat

        let DrawCell ((cellText : string), (row : int), (startPos : Xywh), (textSize : SizeF), (lineColor : MCvScalar) ,(offsetX : int)) =
            let cellWidth = int(textSize.Width + float32 Offset)
            let cellHeight = int(textSize.Height + float32 Offset)
            let x1 = startPos.X + offsetX
            let y1 = startPos.Y + row * cellHeight
            let rect = new Rectangle(x1, y1, cellWidth, cellHeight)
            let mutable backColor = new MCvScalar(0.0, 0.0, 0.0, 225.0)
            if lineColor.V2 = 0.0 then
                backColor.V2 <- 0.0
            else
                backColor.V2 <- 255.0

            CvInvoke.Rectangle(FrontFrame, rect, backColor, -1)
            CvInvoke.Rectangle(FrontFrame, rect, lineColor)
            let textCoord = new PointF(float32(x1 + (cellWidth - int(textSize.Width)) / 2), float32(y1 + (cellHeight - int(textSize.Height) + Offset) / 2))
            TextPainter cellText  textCoord  Color.White 
            new PointF(float32(x1 + cellWidth), float32(y1 + cellHeight))

        let IsWithinBounds (start : PointF) (end' : PointF) (intersection : PointF) =
            let minX = min start.X end'.X
            let maxX = max start.X end'.X
            let minY = min start.Y end'.Y
            let maxY = max start.Y end'.Y

            intersection.X >= minX && intersection.X <= maxX &&
            intersection.Y >= minY && intersection.Y <= maxY

        let FindIntersection (p1 : Point) (q1 : Point) (p2 : Point) (q2 : Point) =
            let a1 = float(q1.Y - p1.Y)
            let b1 = float(p1.X - q1.X)
            let a2 = float(q2.Y - p2.Y)
            let b2 = float(p2.X - q2.X)
            let determinant = a1 * b2 - a2 * b1

            if determinant = 0.0 then None
            else
                let c1 = a1 * float(p1.X) + b1 * float(p1.Y)
                let c2 = a2 * float(p2.X) + b2 * float(p2.Y)
                let intersectX = (b2 * c1 - b1 * c2) / determinant
                let intersectY = (a1 * c2 - a2 * c1) / determinant
                let intersection = Point(int intersectX, int intersectY)

                if IsWithinBounds p1 q1 intersection && IsWithinBounds p2 q2 intersection then Some intersection
                else None

        let GetLongestString (strings : string list) =
            strings |> List.maxBy (fun s -> s.Length)

        let CalcTextSize (text : string) =
            let tempImage = new Mat(_Height, _Width, DepthType.Cv8U, 4)
            let font = new System.Drawing.Font("Arial", float32 SizeFont)
            let bitmap = tempImage.ToBitmap()

            use graphics = System.Drawing.Graphics.FromImage(bitmap)
            let textSize = graphics.MeasureString(text, font)
            new SizeF(textSize.Width, textSize.Height)



        let GetLongestStringSize (strings : string list) =
            let longestString = GetLongestString strings
            let size = CalcTextSize longestString // Assume CalcTextSize is implemented elsewhere
            size

        let CheckPointIsNotInRectangle (lt : Point) (rb : Point) (target : Point) =
            not (Rectangle(lt.X, lt.Y, rb.X - lt.X, rb.Y - lt.Y).Contains(target))

        let FillPoly (p1 : Point) (p2 : Point) (p3 : Point) (p4 : Point) (color : MCvScalar) =
            use vp = new VectorOfPoint([| p1; p2; p3; p4 |])
            CvInvoke.FillConvexPoly(FrontFrame, vp, color)

        let GetOriginalPoint (p : Point) =
            match p with
            | p when p = oflt -> lt
            | p when p = ofrt -> rt
            | p when p = oflb -> lb
            | p when p = ofrb -> rb
            | _ -> p // Default or throw exception if necessary

        let CalculateNewPoint (p : Point) (x : int) (y : int) =
            Point(p.X + x, p.Y + y)

        let DrawOutsideRectangles (p1 : Point) (p2 : Point) (frame : Mat) (lineColor : MCvScalar) (rectColor : MCvScalar) =
            if CheckPointIsNotInRectangle lt rb p1 && CheckPointIsNotInRectangle lt rb p2 then
                let p3 = GetOriginalPoint p2
                let p4 = GetOriginalPoint p1
                FillPoly p1 p2 p3 p4 rectColor
                CvInvoke.Line(frame, p1, p2, lineColor)
                CvInvoke.Line(frame, p2, p3, lineColor)
                CvInvoke.Line(frame, p3, p4, lineColor)
                CvInvoke.Line(frame, p4, p1, lineColor)

        let DrawLines (minX : int) (minY : int) (maxX : int) (maxY : int) (devX : int) (devY : int) (lineColor : MCvScalar) =
            lt <- Point(minX, minY) // Left top
            rt <- Point(maxX, minY) // Right top
            lb <- Point(minX, maxY) // Left bottom
            rb <- Point(maxX, maxY) // Right bottom
            dc <- Point(devX, devY) // Device center
            pc <- Point((maxX + minX) / 2, (maxY + minY) / 2) // Panel center

            if Offset <> 0 then
                let dst = CalcDistance pc ImageCenter 
                let scl = 1 -  Offset / dst
                vTc <- CalculateNewPoint vTc ((ImageCenter.X - pc.X) / dst *  Offset) ((ImageCenter.Y - pc.Y) / dst *  Offset)
                vTp <- CalculateNewPoint vTp ((lt.X - pc.X) * scl) ((lt.Y - pc.Y) * scl)
                oflt <- CalculateNewPoint pc (int32 vTc.X + int32 vTp.X) (int32 vTc.Y + int32 vTp.Y)

        member x.DrawTable (backFrame : Mat, devices : Device seq, newTable:bool) =
            BackFrame <- backFrame

            if newTable then 
                FrontFrame <-  new Mat(BackFrame.Height,BackFrame.Width, DepthType.Cv8U, 4)
                devices
                |> Seq.iter (fun dev ->
                    let mutable points = []
                    let mutable lineColor = MCvScalar(0.0, 0.0, 0.0, 0.0)
                    let mutable longestRowNameSize = SizeF()
                    let mutable longestRowContentSize = SizeF()

                    if String.IsNullOrEmpty dev.ErrorMsg then
                        let GoingCount = string dev.GoingCount
                        let ErrorCount = string dev.ErrorCount
                        let ErrorAvgTime = sprintf "%.2f" dev.ErrorAvgTime
                        let contents = [GoingCount; ErrorCount; ErrorAvgTime]
                        longestRowNameSize <- GetLongestStringSize rowNames
                        longestRowContentSize <- contents |> GetLongestStringSize
                        longestRowContentSize.Height <- longestRowNameSize.Height
                        lineColor <- new MCvScalar(70.0, 128.0, 0.0, 255.0)
                        for i = 0 to 2 do
                            let rowName = rowNames.[i]
                            let rowContent = contents.[i]
                            points <- points @ [DrawCell(rowName, i + 1, dev.Xywh, longestRowNameSize, lineColor, 0)]
                            points <- points @ [DrawCell(rowContent, i + 1, dev.Xywh, longestRowContentSize, lineColor, int longestRowNameSize.Width + Offset)]
                    else
                        longestRowNameSize <- CalcTextSize errorHead
                        longestRowContentSize <- CalcTextSize dev.ErrorMsg
                        longestRowContentSize.Height <- longestRowNameSize.Height
                        lineColor <- new MCvScalar(0.0, 0.0, 255.0, 0.0)
                        points <- points @ [DrawCell(errorHead, 1, dev.Xywh, longestRowNameSize, lineColor, 0)]
                        points <- points @ [DrawCell(dev.ErrorMsg, 1, dev.Xywh, longestRowContentSize, lineColor, int longestRowNameSize.Width + Offset)]

                    let titleSize = CalcTextSize dev.QualifiedName
                    let rowSize = longestRowNameSize + longestRowContentSize
                    let newTitleSizeWidth = if titleSize.Width > rowSize.Width then titleSize.Width else rowSize.Width + (Offset |> float32)
                    let newTitleSize = SizeF(newTitleSizeWidth, longestRowNameSize.Height)

                    points <- points @ [DrawCell(dev.QualifiedName, 0, dev.Xywh, newTitleSize, lineColor, 0)]
                    let maxX = points |> List.map (fun p -> p.X) |> List.max |> int
                    let maxY = points |> List.map (fun p -> p.Y) |> List.max |> int

                    DrawLines dev.Xywh.X dev.Xywh.Y maxX maxY dev.Xywh.W.Value dev.Xywh.H.Value lineColor
                )
                AlphaBlend FrontFrame BackFrame
            else
                AlphaBlend FrontFrame BackFrame
