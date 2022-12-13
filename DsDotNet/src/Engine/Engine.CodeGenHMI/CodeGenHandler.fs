namespace Engine.CodeGenHMI
open Engine.Core
open Newtonsoft.Json;

[<AutoOpen>]
module CodeGenHandler =
    type ParseModel(modelConfig:string) =
        let model = ModelLoader.LoadFromConfig(modelConfig)
        let cpuRes   = GenCpuCode(model)
        let hmiRes   = GenHmiCode(model)
        let pilotRes = GenPilotCode(model)
        let SelectGet target = 
            match target with
            | "cpu"      -> cpuRes
            | "ds-pilot" -> hmiRes
            | "hmi"      -> pilotRes
            | _ -> { 
                    from = null; 
                    succeed = false; 
                    body = null; 
                    error = "target error" 
                }
        member x.CpuResult   = cpuRes
        member x.HmiResult   = hmiRes
        member x.PilotResult = pilotRes
        member x.SelectedResult(target) = SelectGet target
      
    //let jsonSettings = new JsonSerializerSettings()
    //jsonSettings.Converters.Add(
    //    new Newtonsoft.Json.Converters.StringEnumConverter()
    //);
    //let res = ParseModel(@"E:\test_sample\Control_config.json")
    //let jsonString = 
    //    JsonConvert.SerializeObject(res.HmiResult, jsonSettings)
    //printfn "%A" jsonString