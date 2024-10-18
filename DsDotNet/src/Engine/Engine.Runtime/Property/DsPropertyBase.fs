namespace Engine.Runtime

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices
open Newtonsoft.Json
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open Engine.Core
open System.ComponentModel

[<AutoOpen>]
module DsPropertyBaseModule =


    [<JsonObject>]
    [<TypeConverter(typeof<ExpandableObjectConverter>)>]
    type PropertyBase() =
        new(name: string) as this = 
            PropertyBase()
            then this.UpdateProperty(name)

        [<Browsable(false)>] // 속성 창에서 숨기기
        member val Name = getNull<string>() with get, set

        member x._ClassType = x.GetType().Name
        
        override x.ToString() = x._ClassType

        member private x.UpdateProperty(name: string) =
            x.Name <- name



            
[<Extension>]
type DsPropertyExt =

    [<Extension>]
    static member ExportPropertyToJson(path: string, data: obj) =
        let settings = JsonSerializerSettings()
        settings.Formatting <- Formatting.Indented
        settings.TypeNameHandling <- TypeNameHandling.Auto
        let json = JsonConvert.SerializeObject(data, settings)
        File.WriteAllText(path, json)

    [<Extension>]
    static member ImportPropertyFromJson<'T>(path: string) : 'T =
        if File.Exists(path) then
            let json = File.ReadAllText(path)
            let settings = JsonSerializerSettings()
            settings.TypeNameHandling <- TypeNameHandling.Auto
            JsonConvert.DeserializeObject<'T>(json, settings)
        else
            failwith "File not found or invalid path"



