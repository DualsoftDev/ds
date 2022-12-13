namespace Engine.CodeGenHMI

open Engine.Core
open Newtonsoft.Json;

[<AutoOpen>]
module CodeGenHandler =
    type ParseModel(modelConfig:string) =
        let model = ModelLoader.LoadFromConfig(modelConfig)
        let JsonWrapping (result:Initializer) = 
            let jsonSettings = new JsonSerializerSettings()
            jsonSettings.Converters.Add(
                new Newtonsoft.Json.Converters.StringEnumConverter()
            );
            JsonConvert.SerializeObject(result, jsonSettings)
        let SelectGet target = 
            match target with
            | "ds-pilot" -> JsonWrapping(GenPilotCode(model))
            | "ds-hmi"   -> JsonWrapping(GenHmiCode(model))
            | _ -> JsonWrapping { 
                    from    = null; 
                    success = false; 
                    body    = null; 
                    error   = "target error" 
                }
        member x.SelectedResult(target) = SelectGet target

    //let res = ParseModel(@"E:\temp\ds-storage\ds-dev\FactoryIO\FactoryIO.config.json")
    //let res = ParseModel(@"E:\temp\ds-storage\ds-dev\test_sample\Control_config.json")
    //printfn "%A" (res.SelectedResult "ds-hmi")