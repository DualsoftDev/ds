namespace Engine.Import.Office

open Newtonsoft.Json
open System
open System.Collections.Generic

type AniModelConverter() =
    inherit JsonConverter()

  
    override this.CanConvert(objectType : Type) =
        objectType = typeof<AniModel>
    override this.ReadJson(reader : JsonReader, _ : Type, _ : obj, serializer : JsonSerializer) : obj =
        // Temporarily remove the converter
        serializer.Converters.Clear()
        let mutable deviceCache = Dictionary<string, AniDevice>() 
        let mutable apiCache = Dictionary<string, AniApi>()

        let updateOrAddToCache  (cache: Dictionary<string, 'T>) (key:string) getItem updateItem =
            match cache.TryGetValue(key) with
            | true, value -> updateItem value
            | _ -> cache.[key] <- getItem()

        let updateAniLinks (aniLinks:AniLink seq)=
            aniLinks |> Seq.iter (fun link ->
                updateOrAddToCache apiCache (link.Source.DevNApiName) (fun () -> link.Source) (fun api -> link.Source <- api)
                updateOrAddToCache apiCache (link.Target.DevNApiName) (fun () -> link.Target) (fun api -> link.Target <- api)
                updateOrAddToCache deviceCache (link.Source.Device.Name) (fun () -> link.Source.Device) (fun device -> link.Source.Device <- device)
                updateOrAddToCache deviceCache (link.Target.Device.Name) (fun () -> link.Target.Device) (fun device -> link.Target.Device <- device)
            )

        let aniModel = serializer.Deserialize<AniModel>(reader)
        aniModel.AniLinks |> updateAniLinks
        let apis= aniModel.GetApis();
        aniModel.GetDevices() |> Seq.iter(fun d-> 
                                    apis |>Seq.filter(fun f-> f.Device = d)
                                         |>Seq.iter(fun api-> d.AniApis.Add api|>ignore)
                                         )
        aniModel

    override this.WriteJson(writer : JsonWriter, value : obj, serializer : JsonSerializer) =
        serializer.Serialize(writer, value)


module AniModelConvertJson = 
 
    let serializeAniModel (aniModel: AniModel) =
        JsonConvert.SerializeObject(aniModel, Formatting.Indented);
    
    let deserializeAniModel (jsonString: string) =
        let settings = JsonSerializerSettings(ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
        settings.Converters.Add(AniModelConverter())
        JsonConvert.DeserializeObject<AniModel>(jsonString, settings)
