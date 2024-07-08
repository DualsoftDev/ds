namespace T

open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS
open NUnit.Framework
open System
open Newtonsoft.Json

[<AutoOpen>]
module JsonSerializeTestModule =
    type NewtonsoftJson = Newtonsoft.Json.JsonConvert


    // https://stackoverflow.com/questions/25007001/json-net-does-not-preserve-primitive-type-information-in-lists-or-dictionaries-o
    type TypePreservingConverter() =
        inherit JsonConverter()

        override this.CanRead = false

        override this.CanConvert(objectType: Type) = objectType.IsPrimitive || objectType = typeof<Decimal>

        override this.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) : obj =
            raise (NotImplementedException())

        override this.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
            match serializer.TypeNameHandling with
            | TypeNameHandling.All ->
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
            | _ ->
                writer.WriteValue(value)



    /// DB log table 의 row 항목
    type MyORMLog(id: int, storageId: int, at: DateTime, value: obj) =
        new() = MyORMLog(-1, -1, DateTime.MaxValue, null)
        member val Id = id with get, set
        member val StorageId = storageId with get, set
        member val At = at with get, set
        member val Value = value with get, set

    [<TestFixture>]
    type ObjectSerializeTest() =

        let supportedObjects = [| box "Hello world"; 3.14; DateTime.Now; 1234567890L; 1234567890.0; |]
        // serialize 후 복원시
        // 원래의 int, long, ulong => long type 으로 변환
        // float, double, decimal => double type 으로 변환
        let unsupportedObjects = [| box 8y; 8uy; 123s; 123us; 456; 457u; 1234567890; 1234567890UL; 1234567890.0f; 1234567890.0M; |]
        [<Test>]
        member _.``Newtonsoft Object SerializationTest1`` () =
            for o in supportedObjects do
                let log = MyORMLog(1, 2, DateTime.Now, o)
                let json =  NewtonsoftJson.SerializeObject(log)
                let log2 = NewtonsoftJson.DeserializeObject<MyORMLog>(json)
                log.Id === log2.Id
                log.StorageId === log2.StorageId
                log.At === log2.At
                log.Value === log2.Value

            ()
        [<Test>]
        member _.``Newtonsoft Object SerializationTest with type handler`` () =
            let settings = new JsonSerializerSettings(TypeNameHandling = TypeNameHandling.All)
            settings.Converters.Insert(0, new TypePreservingConverter())
            let objects = supportedObjects @ unsupportedObjects
            for o in objects do
                let log = MyORMLog(1, 2, DateTime.Now, o)
                let json =  NewtonsoftJson.SerializeObject(log, settings)
                let log2 = NewtonsoftJson.DeserializeObject<MyORMLog>(json, settings)
                log.Id === log2.Id
                log.StorageId === log2.StorageId
                log.At === log2.At
                log.Value === log2.Value
            ()