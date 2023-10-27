namespace Engine.Core

open System.IO
open Newtonsoft.Json
type DSCommonAppSettings() =
    member val LogDBConnectionString = "" with get, set
    member val HmiWebServer = "" with get, set
    static member Load(jsonPath:string) =
        jsonPath
        |> File.ReadAllText
        |> JsonConvert.DeserializeObject<DSCommonAppSettings>


type ModelCompileInfo(dsConfigJson:string, pptPath:string ) =
    member val PptPath = pptPath with get, set
    member val ConfigPath = dsConfigJson with get, set