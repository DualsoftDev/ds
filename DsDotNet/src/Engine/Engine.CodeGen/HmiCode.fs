namespace Engine.CodeGen

open System.Diagnostics
open System.Collections.Generic
open Engine.Core
open Newtonsoft.Json

[<AutoOpen>]
module HmiGenModule =
  
    let GenHmiCpuText(model:Model) = 
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