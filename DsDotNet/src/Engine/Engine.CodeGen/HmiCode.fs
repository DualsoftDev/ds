namespace Engine.CodeGen

open System.Diagnostics
open System.Collections.Generic
open Engine.Core
open Newtonsoft.Json
open Model.Import.Office

[<AutoOpen>]
module HmiGenModule =
  
    let GenHmiCpuText(model:CoreModule.Model) = 
        let testText = 
            """
            [
                {
                "EmergencyButtons": "A;B;C;D",
                "AutoButtons": "A",
                "StartButtons": "B",
                "ResetButtons": "C;D"
                },
                {
                ...
                }
            ]
            """
       
        testText

    [<EntryPoint>]        
    let main argv = 
        let pptModel  = ImportM.FromPPTX(@"D:\ds\test\ds.pptx")
        let coreModel = ConvertM.ToDs(pptModel);
        let json = GenHmiCpuText(coreModel)

        0

        

     