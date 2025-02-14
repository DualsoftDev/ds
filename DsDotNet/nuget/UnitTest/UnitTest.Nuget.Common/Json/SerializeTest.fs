namespace T

open System
open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
//open Newtonsoft.Json
//open System.Text.Json



type CheckExtension =
    [<Extension>]
    static member SystemTextSerializerCheck<'T>(obj:obj) =
        let s1 = obj |> System.Text.Json.JsonSerializer.Serialize
        let d  = s1  |> System.Text.Json.JsonSerializer.Deserialize<'T>
        let s2 = d   |> System.Text.Json.JsonSerializer.Serialize
        s1 === s2

    [<Extension>]
    static member NewtonSoftSerializerCheck<'T>(obj:obj) =
        let s1 = obj |> Newtonsoft.Json.JsonConvert.SerializeObject
        let d  = s1  |> Newtonsoft.Json.JsonConvert.DeserializeObject<'T>
        let s2 = d   |> Newtonsoft.Json.JsonConvert.SerializeObject
        s1 === s2


[<AutoOpen>]
module SerializeTestModule =

    [<AllowNullLiteral>]
    type private RefClassSample() =
        member val Value = 1 with get, set
        member val Name = "" with get, set

    type RecordSample = {
        Value:int
        Name:string
    }

    type DiscriminatedUnion =
        | String of string
        | Int of int



    [<TestFixture>]
    type SerializeTest() =
        let class1 = RefClassSample(Name="test", Value=2)
        let record1 = { Name="test"; Value=2 }
        let str1 = String "Hello"
        let dict1 = [(1, "one"); (2, "two"); (3, "three")] |> Tuple.toDictionary
        let resizeArray = ResizeArray<int>([|1;2;3;4;5|])

        [<Test>]
        member _.SystemTextClassSerializeTest() =
            class1.SystemTextSerializerCheck<RefClassSample>()
            record1.SystemTextSerializerCheck<RecordSample>()
            //str1.SystemTextSerializerCheck<DiscriminatedUnion>()  // Fail: 'F# discriminated union serialization is not supported
            dict1.SystemTextSerializerCheck<Dictionary<int, string>>()
            resizeArray.SystemTextSerializerCheck<ResizeArray<int>>()

            class1.NewtonSoftSerializerCheck<RefClassSample>()
            record1.NewtonSoftSerializerCheck<RecordSample>()
            str1.NewtonSoftSerializerCheck<DiscriminatedUnion>()    // OK
            dict1.NewtonSoftSerializerCheck<Dictionary<int, string>>()
            resizeArray.NewtonSoftSerializerCheck<ResizeArray<int>>()
