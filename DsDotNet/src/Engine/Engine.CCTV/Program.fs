open System.IO
open System


[<STAThread>]
[<EntryPoint>]
let main _ = 
    let outputFileName = "outputChart.jpg"
    //PlotlyChart.visualizeAndSaveImage outputFileName |> ignore
    OxyChart.visualizeAndSaveImage outputFileName  |> ignore
    0 // 반환값으로 성공적으로 완료되었음을 나타냅니다.
    