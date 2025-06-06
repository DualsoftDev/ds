namespace T

open Dual.Common.Base.FS
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open NUnit.Framework
open System
open Newtonsoft.Json
open FSharp.Json
open Dual.Common.Base.FS.SampleDataTypes

(*
    ------- F# JSON serialization test -------
    1. chiron: json computation expression 이용?
     - https://www.codesuji.com/2019/01/07/Chiron-Introduction/
     - https://github.com/xyncro/chiron
    2. Newtonsoft.Json.FSharp.JsonConvert 사용
    3. FSharp.Json 사용
     - https://github.com/fsprojects/FSharp.Json
*)


type NewtonsoftJson = Newtonsoft.Json.JsonConvert
//type NewtonsoftFSharp = Newtonsoft.Json.FSharp.JsonConvert
type SystemTextJson = System.Text.Json.JsonSerializer
//type FSharpJson = FSharp.Json.Json

/// DB log table 의 row 항목
type MyORMLog(id: int, storageId: int, at: DateTime, value: obj) =
    new() = MyORMLog(-1, -1, DateTime.MaxValue, null)
    member val Id = id with get, set
    member val StorageId = storageId with get, set
    member val At = at with get, set
    member val Value = value with get, set

type FSharpRecord = {
    Name  : string
    Value : obj
    Message : string
    Tuple: obj*string
}

    type TheRecord = {
        Value: string
    }

    type TheUnion =
        | NoFieldCase
        | OneFieldCase of string
        | ManyFieldsCase of string*int
        | RecordCase of TheRecord

    type SimpleDU =
        | Case1
        | Case2
        | Case3

    type EnumDU =
        | Enum1 = 1
        | Enum2 = 2
        | Enum3 = 3

    type OtherRecord = {
        Union: TheUnion
    }



[<AutoOpen>]
module JsonSerializeTestModule =
    [<TestFixture>]
    type ObjectSerializeTest() =

        let supportedObjects = [| box "Hello world"; 3.14; DateTime.Now; 1234567890L; 1234567890.0; |]
        // serialize 후 복원시
        // 원래의 int, long, ulong => long type 으로 변환
        // float, double, decimal => double type 으로 변환
        let unsupportedObjects = [| box 8y; 8uy; 123s; 123us; 456; 457u; 1234567890; 1234567890UL; 1234567890.0f; 1234567890.0M; |]

        let record = {Name="kwak"; Value=3.14; Message="Hello"; Tuple=(3, "three")}


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
            settings.Converters.Insert(0, new ObjTypePreservingConverter([|"Value"|]))
            let objects = supportedObjects @ unsupportedObjects
            for o in objects do
                let log = MyORMLog(1, 2, DateTime.Now, o)
                let json =  NewtonsoftJson.SerializeObject(log, settings)
                let log2 = NewtonsoftJson.DeserializeObject<MyORMLog>(json, settings)
                log.Id === log2.Id
                log.StorageId === log2.StorageId
                log.At === log2.At
                log.Value === log2.Value
                tracefn "%s" json
            ()

        [<Test>]
        member _.``NewtonSoft FSharpRecordSerializeTest``() =
            let str = NewtonsoftJson.SerializeObject(record)
            let xx = NewtonsoftJson.DeserializeObject<FSharpRecord>(str)
            let str2 = NewtonsoftJson.SerializeObject(xx)
            str === str2

        [<Test>]
        member _.``SystemText FSharpRecordSerializeTest``() =
            let str = SystemTextJson.Serialize(record)
            let xx = SystemTextJson.Deserialize<FSharpRecord>(str)
            let str2 = SystemTextJson.Serialize(xx)
            str === str2


        [<Test>]
        member _.``NewtonSoft FSharp DU SerializeTest``() =
            let dus = [
                OneFieldCase "one"
                ManyFieldsCase ("many", 3)
                RecordCase {Value="record"}
                NoFieldCase
            ]
            let str = NewtonsoftJson.SerializeObject(dus)
            let xx = NewtonsoftJson.DeserializeObject<TheUnion[]>(str)
            let str2 = NewtonsoftJson.SerializeObject(xx)
            str === str2




    [<TestFixture>]
    type FSharpJsonSerializeTest() =
        let record = {Name="kwak"; Value=3.14; Message="Hello"; Tuple=(3, "three")}

        [<Test>]
        member _.``FSharpJson FSharpRecordSerializeTest``() =
            let str = FSharpJson.Serialize record
            let xx:FSharpRecord = FSharpJson.Deserialize<FSharpRecord> str
            let str2 = FSharpJson.Serialize xx
            str === str2

        [<Test>]
        member _.``FSharpJson Discriminated Union Test``() =
            let dus = [
                OneFieldCase "one"
                ManyFieldsCase ("many", 3)
                RecordCase {Value="record"}
                NoFieldCase
            ]
            let str = FSharpJson.Serialize dus
            let xx = FSharpJson.Deserialize<TheUnion[]> str
            let str2 = FSharpJson.Serialize xx
            str === str2

        [<Test>]
        member _.``FSharpJson Simple Discriminated Union Test``() =
            let dus = [Case1; Case2; Case3]
            let str = FSharpJson.Serialize dus
            let xx = FSharpJson.Deserialize<SimpleDU[]> str
            let str2 = FSharpJson.Serialize xx
            str === str2

        [<Test>]
        member _.``FSharpJson Enum Discriminated Union Test``() =
            let dus = [EnumDU.Enum1; EnumDU.Enum2; EnumDU.Enum3]
            let str = FSharpJson.Serialize dus
            let xx = FSharpJson.Deserialize<EnumDU[]> str
            let str2 = FSharpJson.Serialize xx
            str === str2


        /// Polymorphism 은 지원하지 않음: FSharp.Json.JsonSerializationError: 'Unknown type: Student'
        [<Test>]
        member _.``FSharpJson Object Polymorphism Test``() =
            try
                let people:Person list = [
                    Student("kwak", 20)
                    Teacher("kim", 30)
                ]
                let str = FSharpJson.Serialize people
                let xx = FSharpJson.Deserialize<Person[]> str
                let str2 = FSharpJson.Serialize xx
                str === str2
            with ex ->
                tracefn "%s" ex.Message



    type ExStudent(name:string, age:int, union:TheUnion) =
        inherit Student(name, age)
        member x.Union = union

    /// FSharpJson (FSharp.Json) 은 class 를 지원하지 않음
    /// Newtonsoft.Json : Discriminated Union 을 지원하는 듯.
    /// F# type 에 대해서 converter 를 적용하고, 나머지는 Newtonsoft.Json 에서 처리
    [<TestFixture>]
    type MixedModeFSharpJsonSerializeTest() =
        let settings = new JsonSerializerSettings()
        do
            settings.Converters.Add(FSharpJsonConverter<TheUnion>())

        [<Test>]
        member _.``FSharpJson Object With Inner FSharp Member Test``() =
            let exStudent = ExStudent("kwak", 20, OneFieldCase "one")
            let str = NewtonsoftJson.SerializeObject(exStudent, settings)
            let strXxx = NewtonsoftJson.SerializeObject(exStudent, getNull<JsonSerializerSettings>())
            let xx = NewtonsoftJson.DeserializeObject<ExStudent>(str, settings)
            let str2 = NewtonsoftJson.SerializeObject(xx, settings)
            str === str2
