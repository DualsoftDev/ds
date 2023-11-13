namespace Engine.CodeGenHMI

open Engine.Core
open Newtonsoft.Json
open Engine.Parser.FS

[<AutoOpen>]
module CodeGenHandler =
    let JsonWrapping (result: Initializer) =
        let jsonSettings = new JsonSerializerSettings()
        jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter())
        JsonConvert.SerializeObject(result, jsonSettings)

    type ParseModel(modelConfig: string) =
        let model = ParserLoader.LoadFromConfig(modelConfig)

        let SelectGet target =
            match target with
            //| "ds-pilot" -> JsonWrapping(GenPilotCode(model))
            | "ds-hmi" -> JsonWrapping(HmiCode(model).Generate())
            | _ ->
                JsonWrapping
                    { from = null
                      success = false
                      body = null
                      error = "target error" }

        member x.SelectedResult(target) = SelectGet target

//let res = ParseModel(@"E:/temp/ds-storage/ds-dev/FactoryIO/FactoryIO.config.json")
//let res = ParseModel(@"E:/temp/ds-storage/ds-dev/test_sample/Control_config.json")
//"ds-hmi" |> res.SelectedResult |> printfn "%A"
