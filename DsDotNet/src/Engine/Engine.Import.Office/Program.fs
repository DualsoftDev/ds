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
        let pptParms:PPTParams = {TargetType = WINDOWS; AutoIOM = true; CreateFromPPT = false; CreateBtnLamp = true}


        clearNFullSlotHwSlotDataTypes()
        let dsPPT = ImportPPT.GetDSFromPPTWithLib (testPath, false, pptParms)

        0