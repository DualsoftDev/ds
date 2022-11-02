namespace Engine.CodeGen

open System.Diagnostics
open System.Collections.Generic
open Engine.Core
open Newtonsoft.Json

[<AutoOpen>]
module CpuGenModule =
    let GenCpuCode(model:Model) = 
        let testText = 
            """
            [
                {
                "GateName": "GateAND",
                "Out": "O1",
                "In1": "A;B;C;D"
                },
                {
                "GateName": "GateSR",
                "Out": "O1",
                "In1": "A;!B;C;D",
                "In2": "E;F"
                }
            ]
            """
       
        testText