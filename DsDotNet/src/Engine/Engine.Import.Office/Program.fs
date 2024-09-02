namespace Engine.Runtime

open Engine.Core

open Engine.Import.Office
module testMain =

    [<EntryPoint>]
    let main _ =
        let testPath = "F:/Git/ds/DsDotNet/bin/net8.0-windows/HelloDS.pptx";
        let testPath = "F:/DsModeling/Side9/Side9.pptx";
        let pptParms:PptParams = {TargetType = (WINDOWS); DriverIO = (LS_XGK_IO); AutoIOM = true; CreateFromPpt = false; CreateBtnLamp = true}


        clearNFullSlotHwSlotDataTypes()
        let dsPpt = ImportPpt.GetDSFromPptWithLib (testPath, false, pptParms)

        0