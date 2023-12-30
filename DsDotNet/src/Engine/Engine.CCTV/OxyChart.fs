module OxyChart 
open OxyPlot
open OxyPlot.Series
open OxyPlot.Axes
open OxyPlot.SkiaSharp
open System.Drawing
open System.IO


let visualizeImage () =
    let model = PlotModel(Title = "Cake Type Popularity")
    let rand = System.Random()
    let cakePopularity = Array.zeroCreate<float> 5

    for i in 0..4 do
        cakePopularity.[i] <- rand.NextDouble()

    let sum = cakePopularity |> Array.sum

    let barSeries = 
        BarSeries(
            ItemsSource = 
                [ for i in 0..4 ->
                    BarItem(Value = (cakePopularity.[i] / sum * 100.0)) ])
        
    barSeries.LabelPlacement <- LabelPlacement.Inside
    barSeries.LabelFormatString <- "{0:.00}%"

    model.Series.Add(barSeries)

    model.Axes.Add(
        CategoryAxis(
            Position = AxisPosition.Left,
            Key = "CakeAxis",
            ItemsSource = 
                [|
                    "Apple cake"
                    "Baumkuchen"
                    "Bundt Cake"
                    "Chocolate cake"
                    "Carrot cake"
                |]))
    let memoryStream = new MemoryStream()
    PngExporter.Export(model, memoryStream,600, 400)
    memoryStream
   
let visualizeAndSaveImage (outputFilePath) =
    let memoryStream = visualizeImage()
    let bitmap = new System.Drawing.Bitmap(memoryStream);
    bitmap.Save(outputFilePath)
