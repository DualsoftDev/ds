namespace Dual.Common.Core.FS

open FSharp.Json
open Newtonsoft.Json
open System
open Microsoft.FSharp.Reflection


[<AutoOpen>]
module FSharpJsonModule =
    ()

/// FSharp type JSON serialization
type FSharpJson =
    // obj 을 포함하고 있으므로 allowUntyped 를 설정해 주어야 함.  그러지 않으면 --> FSharp.Json.JsonSerializationError: 'Failed to serialize untyped data, allowUntyped set to false'
    // https://github.com/fsprojects/FSharp.Json/blob/master/FSharp.Json/InterfaceTypes.fs
    static let jsonSettings =
        JsonConfig.create(
            serializeNone=SerializeNone.Null,
            deserializeOption=DeserializeOption.RequireNull,
            allowUntyped=true,
            enumValue=EnumMode.Name
            )

    /// F# serialize
    static member Serialize(jobj:obj) = Json.serializeEx jsonSettings jobj
    /// F# deserialize
    static member Deserialize<'T>(json:string):'T = Json.deserializeEx<'T> jsonSettings json



type FSharpJsonConverter<'T>() =
    inherit JsonConverter()

    override this.CanConvert(t: Type) =
        FSharpType.IsUnion(t)  // || FSharpType.IsRecord(t) || FSharpType.IsTuple(t) || FSharpType.IsEnum(t)

    //override this.ReadJson(reader: JsonReader, t: Type, existingValue: obj, serializer: JsonSerializer) =
    //    let json = reader.ReadAsString() // JSON을 문자열로 읽기
    //    FSharpJson.Deserialize<'T> json :> obj // FSharp.Json으로 역직렬화

    override this.ReadJson(reader: JsonReader, t: Type, existingValue: obj, serializer: JsonSerializer) =
        // 전체 JSON 객체를 읽기
        let jsonObject = Newtonsoft.Json.Linq.JToken.Load(reader)
        // FSharp.Json으로 역직렬화
        jsonObject.ToString() |> FSharpJson.Deserialize<'T> :> obj

    override this.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
        let json = FSharpJson.Serialize value // FSharp.Json으로 직렬화
        writer.WriteRawValue(json) // 원시 JSON 값을 기록