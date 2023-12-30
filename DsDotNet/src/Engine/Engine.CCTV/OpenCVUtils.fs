module OpenCVUtils

open Emgu.CV
open Emgu.CV.CvEnum
open Emgu.CV.Structure
open System.Data
open System.IO

open System.Drawing

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

    static member GetSampleDataTable(rowCnt : int) : DataTable =
        let table = new DataTable("SampleTable")
        table.Columns.Add("ID", typeof<int>) |> ignore
        table.Columns.Add("Age", typeof<int>) |> ignore
        table.Columns.Add("Num", typeof<int>) |> ignore
        table.Columns.Add("Name", typeof<string>) |> ignore

        for i = 0 to rowCnt - 1 do
            table.Rows.Add(table.Rows.Count, i % 3, i % 7, $"DS{i}")  |> ignore

        table
