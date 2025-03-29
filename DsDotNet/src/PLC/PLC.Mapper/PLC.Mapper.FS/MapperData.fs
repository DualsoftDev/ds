namespace PLC.Mapper.FS

open System.Collections.Generic
open System.Xml.Serialization
open System.Diagnostics
open System.Runtime.Serialization
open Newtonsoft.Json
open System
open System.IO

module MapperDataModule =

    [<AllowNullLiteral>]
    type DeviceApi() =
        member val Group = "" with get, set
        member val Device = "" with get, set
        member val Api = "" with get, set
        member val Tag = "" with get, set
        member val Color = 0 with get, set
        member val Address = "" with get, set


    type DsApiTag() =
        member val Case      = ""  with get, set
        member val Flow      = ""  with get, set
        member val Name      = ""  with get, set
        member val DataType  = ""  with get, set
        member val Input     = ""  with get, set
        member val Output    = ""  with get, set
        member val SymbolIn  = ""  with get, set
        member val SymbolOut = ""  with get, set

    [<Flags>]
    type UserTagColumn =
    | Case      = 0000 
    | Flow      = 0001
    | Name      = 0002 
    | DataType  = 0003 
    | Input     = 0004 
    | Output    = 0005 
    | SymbolIn  = 0006
    | SymbolOut = 0007

    type DsApiTagConfig = {
        UserTags: DsApiTag array
    }

    let createDefaultUserTagConfig() =
        { 
           UserTags = [||]
        }
    let private jsonSettings = JsonSerializerSettings()

    let LoadDsApiTagConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<DsApiTagConfig>(json, jsonSettings)

    let SaveDsApiTagConfigWithPath (path: string) (cfg: DsApiTagConfig) =
        let json = JsonConvert.SerializeObject(cfg, jsonSettings)
        File.WriteAllText(path, json)
        path



    [<AllowNullLiteral>]
    [<XmlRoot("PowerPointMapper")>]
    type MapperData() =
        member val DeviceApisProp = new List<DeviceApi>() with get, set
        member val TagsProp = new List<DsApiTag>() with get, set

