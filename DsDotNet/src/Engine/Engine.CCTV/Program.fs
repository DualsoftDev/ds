open System.Linq
open System
open System.Drawing


[<STAThread>]
[<EntryPoint>]
let main _ = 

    let dsStreaming = DsStreamingModule.DsStreaming()
    let datas, xywh = OxyImgUtils.createBoxImage ("deviceA \n전진 이상", Rectangle( 400, 300, 200, 200), Color.OrangeRed) 
    let datas, xywh = OxyImgUtils.createPieChartImage ("deviceA", Rectangle(14123, 123, 400, 400), 213, 14) 
    let bitmap = new System.Drawing.Bitmap(datas)

    bitmap.Save("outputChart.jpg")

    0 // 반환값으로 성공적으로 완료되었음을 나타냅니다.
    