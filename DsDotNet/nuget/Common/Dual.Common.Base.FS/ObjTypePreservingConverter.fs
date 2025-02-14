namespace Dual.Common.Base.FS
open System
open Newtonsoft.Json
open System.Collections.Generic



// https://stackoverflow.com/questions/25007001/json-net-does-not-preserve-primitive-type-information-in-lists-or-dictionaries-o
/// Serialize 할 때 type 정보를 같이 저장하는 converter.
/// - objFieldNames 에 포함된 object field 를 갖는 경우, 해당 object 의 type 정보를 같이 저장한다.
/// - objFieldNames 가 empty 이면 모든 field 에 대해 type 정보를 같이 저장한다.
type ObjTypePreservingConverter(objFieldNames: string seq) =
    inherit JsonConverter()
    let objFieldNames = objFieldNames |> HashSet

    new() = ObjTypePreservingConverter([])


    override this.CanRead = false

    override this.CanConvert(objectType: Type) = objectType.IsPrimitive || objectType = typeof<Decimal>

    override this.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) : obj =
        raise (NotImplementedException())

    override this.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
        if isNull(objFieldNames) || objFieldNames.Count = 0 || objFieldNames.Contains(writer.Path) then
            writer.WriteStartObject()
            writer.WritePropertyName("$type", false)
            match serializer.TypeNameAssemblyFormatHandling with
            | TypeNameAssemblyFormatHandling.Full ->
                writer.WriteValue(value.GetType().AssemblyQualifiedName)
            | _ ->
                writer.WriteValue(value.GetType().FullName)
            writer.WritePropertyName("$value", false)
            writer.WriteValue(value)
            writer.WriteEndObject()
        else
            writer.WriteValue(value)
