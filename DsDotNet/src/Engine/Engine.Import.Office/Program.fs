namespace Engine.Runtime

open Engine.Core

open Engine.Import.Office
module testMain =

    [<EntryPoint>]
    let main _ =
        let testPath = "F:/Git/ds/DsDotNet/bin/net8.0-windows/HelloDS.pptx";
        let testPath = "F:/DsModeling/Side9/Side9.pptx";
        let pptParms:PptParams = {HwTarget = getDefaltHwTarget(); AutoIOM = true; CreateFromPpt = false; CreateBtnLamp = true; StartMemory = 1000; OpMemory = 100}
        let modelConfig = createDefaultModelConfig()    


        let dsPpt = ImportPpt.GetDSFromPptWithLib (testPath, false, pptParms, modelConfig)

        0