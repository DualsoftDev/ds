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
open System.Reactive.Subjects

[<AutoOpen>]
module DsPropertyBaseModule =

    [<JsonObject>]
    [<TypeConverter(typeof<ExpandableObjectConverter>)>]
    type PropertyBase(?name: string) as this =
        let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
        let mutable name = defaultArg name ""
        let mutable fqdnObject :FqdnObject option = None

        interface INotifyPropertyChanged with
            [<CLIEvent>]
            member _.PropertyChanged = propertyChanged.Publish

        member x.Name 
            with get() = name
            and set(v) = if name <> v then
                            name <- v
                            this.OnPropertyChanged(nameof x.Name)

        [<Browsable(false)>] // 속성 창에서 숨기기
        member x._ClassType = x.GetType().Name
        [<Browsable(false)>] // 속성 창에서 숨기기
        member val FqdnObject = fqdnObject with get, set

        override x.ToString() = ""//x._ClassType

        // Helper method to trigger PropertyChanged event
        member this.OnPropertyChanged([<CallerMemberName>] ?propertyName: string) =
            let propertyName = defaultArg propertyName ""
            propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

        // Helper method to update property and trigger event
        member x.UpdateField<'T when 'T: equality>(field: byref<'T>, value: 'T, [<CallerMemberName>] ?propertyName: string) =
            let propertyName = defaultArg propertyName (nameof value)
            if field <> value then
                field <- value
                this.OnPropertyChanged(propertyName)
 

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



