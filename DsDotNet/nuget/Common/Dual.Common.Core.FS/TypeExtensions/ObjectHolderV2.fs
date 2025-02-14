namespace Dual.Common.Core.FS

open System
open Newtonsoft.Json
open Dual.Common.Base.FS
open Newtonsoft.Json.Linq


[<AutoOpen>]
module rec ObjectHolderV2Module =

    type ObjectHolderV2 (typ: Type, ?value: obj) =
        member val TypeName = typ.Name with get
        //member val Value = value |? null with get, set
        member val Value = defaultArg value null with get, set
        [<JsonIgnore>] member x.Type: Type = typ // Type 정보를 반환

        new () = ObjectHolderV2(typeof<obj>, null)

        static member internal JsonSettings =
            let settings = JsonSerializerSettings()
            settings.TypeNameHandling <- TypeNameHandling.None  // TypeName 자동 추가 방지
            settings.NullValueHandling <- NullValueHandling.Ignore
            settings.Converters.Add(ObjectHolderConverter())
            settings

    [<CustomJsonConverter("ObjectHolderConverter")>]
    type ObjectHolderConverter() =
        inherit JsonConverter<ObjectHolderV2>()

        override this.WriteJson(writer, value:ObjectHolderV2, serializer) =
            let obj = JObject()
            obj["TypeName"] <- JToken.FromObject(value.TypeName)
            match value.Value with
            | null -> obj["Value"] <- JValue.CreateNull()
            | _ -> obj["Value"] <- JToken.FromObject(value.Value, serializer)
            obj.WriteTo(writer)

        override this.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer) =
            let obj = JObject.Load(reader)
            let typeName = obj["TypeName"].ToObject<string>()
            /// 간소화된 TypeName을 실제 Type으로 변환
            let typ =
                match typeName with
                | "Int32"       -> typeof<int>
                | "UInt32"      -> typeof<uint32>
                | "Int16"       -> typeof<int16>
                | "UInt16"      -> typeof<uint16>
                | "Int64"       -> typeof<int64>
                | "UInt64"      -> typeof<uint64>
                | "Byte"        -> typeof<byte>
                | "SByte"       -> typeof<sbyte>
                | "Single"      -> typeof<single>  // float32
                | "Double"      -> typeof<double>  // float64
                | "Decimal"     -> typeof<decimal>
                | "Boolean"     -> typeof<bool>
                | "Char"        -> typeof<char>
                | "String"      -> typeof<string>
                | "DateTime"    -> typeof<DateTime>
                | "Guid"        -> typeof<Guid>
                | _ -> Type.GetType(typeName)  // 그 외 타입은 기존 방식 유지

            let value =
                match obj.TryGetValue("Value") with
                | (true, token) when token.Type <> JTokenType.Null -> token.ToObject(typ, serializer)
                | _ -> null
            ObjectHolderV2(typ, value)