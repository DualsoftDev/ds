module OpenCVUtils

open Emgu.CV
open Emgu.CV.CvEnum
open Emgu.CV.Structure
open System.Data
open System.IO
open System.Drawing
open Engine.Core
open System


let rect (xywh:Xywh) = Rectangle(xywh.X, xywh.Y //w, h 없을시에 300 기본값
                , if xywh.W.HasValue then xywh.W.Value else 300
                , if xywh.H.HasValue then xywh.H.Value else 300)

[<AutoOpen>]
type OpenCVUtils() =

    static member AlphaBlend (front : Mat, back : Mat) : Mat =
        let result = new Mat()
        CvInvoke.AddWeighted(front, 0.6, back, 0.4, 0.0, result)
        result
 
    static member ResizeImage(img : Mat, newWidth : int, newHeight : int) : Mat =
        let resizedImage = new Mat()
        CvInvoke.Resize(img, resizedImage, new Size(newWidth, newHeight), interpolation = Inter.Lanczos4)
        resizedImage

    static member CompressImage(frame : Mat) : byte[] =
        use stream = new MemoryStream()
        frame.ToImage<Bgr, byte>().ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg)
        frame.Dispose()
        stream.ToArray()

    static member ByteArrayToMat(bytes : byte[]) : Mat =
        use stream = new MemoryStream(bytes)
        use image = new Bitmap(stream)
        image.ToMat().ToImage<Bgr, byte>().Mat
  
    static member CombineImages (totalSize: Size) (images: seq<MemoryStream * Rectangle>) : Mat =
        // 새로운 비트맵 생성
        use combinedImage = new Bitmap(totalSize.Width, totalSize.Height)
    
        // 이미지를 그리는 작업 수행
        use g = Graphics.FromImage(combinedImage)
        for (stream, rect) in images do
            use image = new Bitmap(stream)
            g.DrawImage(image, rect)
            image.Dispose()
        //combinedImage.Save("outputCombinedImage.jpg")
        combinedImage.ToMat().ToImage<Bgr, byte>().Mat

