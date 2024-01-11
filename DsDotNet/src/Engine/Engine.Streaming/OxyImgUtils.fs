module OxyImgUtils 

open OxyPlot
open OxyPlot.Series
open OxyPlot.SkiaSharp
open System.IO
open System.Drawing
open OxyPlot.Annotations

let oxyColor (color:Color) =  OxyColor.FromUInt32(color.ToArgb()|>uint)

let createBoxImage (name: string, rect:Rectangle, backColor:Color) =
    let model = PlotModel()
    let width = rect.Width * 2
    model.PlotMargins <- new OxyThickness(-10);
    // 직사각형을 그리기 위한 BarSeries 추가
    model.Series.Add(BarSeries(ItemsSource = [BarItem(Value = width)]))
    // 직사각형을 그리기 위한 RectangleAnnotation 추가
    model.Annotations.Add(RectangleAnnotation(Fill = (backColor |> oxyColor)))
    // 텍스트 추가
    let ta = TextAnnotation(Text = name, TextColor = (Color.SkyBlue |> oxyColor))

    ta.FontWeight <- FontWeights.Bold // 텍스트 굵기 설정
    ta.Font <- (new Font("Tahoma", 1.0f)).ToString() // 폰트 설정
    ta.FontSize <- 30
    ta.TextVerticalAlignment <- VerticalAlignment.Middle
    ta.TextPosition <- new DataPoint((width|>float)/2.0, 0.0) // 텍스트 위치 설정
    ta.Padding <- OxyThickness(width)
    model.Annotations.Add(ta)

    // 모델 크기 설정
    model.PlotAreaBorderColor <- Color.White |> oxyColor // 플롯 영역 테두리 색상 설정
    model.PlotAreaBorderThickness <- new OxyThickness(2.0) // 플롯 영역 테두리 두께 설정

    // 이미지 출력
    let memoryStream = new MemoryStream()
    PngExporter.Export(model, memoryStream, width, rect.Height)
    memoryStream, rect

let createPieChartImage (name: string,  rect:Rectangle, runCnt:int, errCnt:int) =
    let model = PlotModel(Title = name)
    model.TitleColor <- Color.DarkOrange |> oxyColor
    model.TitleFontSize <- 30;
    let pieSeries = 
        PieSeries(
            StartAngle = 0.0,
            InsideLabelPosition = 0.3,
            AngleSpan = 360.0,
            Diameter = 1.0,
            StrokeThickness = 0.00,
            FontSize = 20,
            InsideLabelFormat = "{0}\n{1}",
            OutsideLabelFormat = null,// 내부 라벨만 표시하도록 설정
            InsideLabelColor = (Color.White |> oxyColor)
        )

    pieSeries.Slices.Add(new PieSlice("RUN", float runCnt))
    pieSeries.Slices.Add(new PieSlice("ERR", float errCnt))
    model.Series.Add(pieSeries)
   
    let memoryStream = new MemoryStream()
    PngExporter.Export(model, memoryStream, rect.Width, rect.Height)
    memoryStream, rect

