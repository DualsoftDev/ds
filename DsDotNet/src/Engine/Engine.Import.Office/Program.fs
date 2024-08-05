namespace Engine.Runtime

open Engine.Core
open Engine.Parser.FS

open System
open System.Reflection
open System.IO
open Engine.Import.Office
open Engine.Core
open Engine.CodeGenCPU
module testMain =

    [<EntryPoint>]
    let main _ =
        let testPath = "F:/Git/ds/DsDotNet/bin/net7.0-windows/HelloDS.pptx";
        let testPath = "F:/DsModeling/Side9/Side9.pptx";
        let pptParms:PptParams = {TargetType = WINDOWS; AutoIOM = true; CreateFromPpt = false; CreateBtnLamp = true}


        clearNFullSlotHwSlotDataTypes()
        let dsPpt = ImportPpt.GetDSFromPptWithLib (testPath, false, pptParms)

        0