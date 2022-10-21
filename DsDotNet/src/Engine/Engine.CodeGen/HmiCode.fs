namespace Engine.CodeGen

open System.Diagnostics
open System.Collections.Generic
open Engine.Core
open Newtonsoft.Json
open Engine.Parser

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
    
        let helper = ModelParser.ParseFromString2(Program.EveryScenarioText, ParserOptions.Create4Simulation());
        let model = helper.Model;
        let xxx = model.ToDsText();

        let json = GenHmiCpuText(model)

        0

        

     