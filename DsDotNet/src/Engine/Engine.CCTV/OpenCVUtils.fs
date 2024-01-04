module OpenCVUtils

open Emgu.CV
open Emgu.CV.CvEnum
open Emgu.CV.Structure
open System.Data
open System.IO
open System.Drawing
open Engine.Core


let rect (xywh:Xywh) = Rectangle(xywh.X, xywh.Y //w, h 없을시에 200 기본값
                , if xywh.W.HasValue then xywh.W.Value else 200
                , if xywh.H.HasValue then xywh.H.Value else 200)

[<AutoOpen>]
type OpenCVUtils() =
 
    static member AlphaBlend(front : Mat, back : Mat) : Mat =
        let result = new Mat()
        CvInvoke.AddWeighted(front, 0.5, back, 0.5, 0.0, result) // 알파 블렌딩 수행
        result

    static member AlphaBlend(front : Mat, locationFront : Point, back : Mat) : Mat =
        if locationFront.X + front.Width > back.Width || locationFront.Y + front.Height > back.Height then
            back.Clone()
        else
            let modifiedBack = back.Clone()
            let roi = new Rectangle(locationFront, front.Size)
            let regionOfInterest = new Mat(modifiedBack, roi)
            CvInvoke.AddWeighted(front, 0.5, regionOfInterest, 0.5, 0.0, regionOfInterest)
            modifiedBack

    static member ResizeImage(img : Mat, newWidth : int, newHeight : int) : Mat =
        let resizedImage = new Mat()
        CvInvoke.Resize(img, resizedImage, new Size(newWidth, newHeight), interpolation = Inter.Lanczos4)
        resizedImage

    static member CompressImage(frame : Mat) : byte[] =
        use stream = new MemoryStream()
        frame.ToImage<Bgr, byte>().ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg)
        stream.ToArray()


    static member CombineImages (totalSize: Size) (images: seq<MemoryStream * Rectangle>) : Mat =
        // 새로운 비트맵 생성
        let combinedImage = new Bitmap(totalSize.Width, totalSize.Height)
    
        // 이미지를 그리는 작업 수행
        use g = Graphics.FromImage(combinedImage)
        for (stream, rect) in images do
            use image = new Bitmap(stream)
            g.DrawImage(image, rect)
        //combinedImage.Save("outputCombinedImage.jpg")
        combinedImage.ToMat().ToImage<Bgr, byte>().Mat

