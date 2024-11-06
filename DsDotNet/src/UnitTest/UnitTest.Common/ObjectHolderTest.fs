namespace T

open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open NUnit.Framework
open System
open Newtonsoft.Json


[<AutoOpen>]
module ObjectHolderTestModule =

    type NaiveHolder(value:obj) =
        new() = NaiveHolder(null)
        member val Value = value with get, set

    [<TestFixture>]
    type ObjectHolderTest() =

        [<Test>]
        member _.DefaultSerializeTest() =
            let x = ObjectHolder()
            let xxx = System.Text.Json.JsonSerializer.Serialize(ObjectHolderType.Undefined)
            let un64 = NaiveHolder(1234567890UL)
            let strUn64 = System.Text.Json.JsonSerializer.Serialize(un64)
            let yyy = JsonConvert.SerializeObject(un64)
            let un64_ = System.Text.Json.JsonSerializer.Deserialize<NaiveHolder>(strUn64)
            let jsUn64_ = JsonConvert.DeserializeObject<NaiveHolder>(strUn64)
            //un64_.Value.GetType() =!= typeof<int>
            //un64_.Value.GetType() === typeof<JsonElement>
            ()

        [<Test>]
        member _.ObjectHolderSerializeTest() =
            let serializeTest v =
                let holder = ObjectHolder.Create(v)
                let str1 = JsonConvert.SerializeObject holder
                let str2 = holder.Serialize()
                str1 === str2
                ObjectHolder.Deserialize(str1).Serialize() === str1


            serializeTest 1234567890123456789UL
            serializeTest 123456789L
            serializeTest 123456789u
            serializeTest 123456789
            serializeTest 1234us
            serializeTest 1234s
            serializeTest false
            serializeTest 'a'
            serializeTest 255uy
            serializeTest "hello, world"


        [<Test>]
        member _.ObjectHolderSerializeValueTest() =

            ObjectHolder.Create(1234567890123456789UL).GetValue().GetType() === typeof<uint64>
            ObjectHolder.Create(123456789L)           .GetValue().GetType() === typeof<int64>
            ObjectHolder.Create(123456789u)           .GetValue().GetType() === typeof<uint32>
            ObjectHolder.Create(123456789)            .GetValue().GetType() === typeof<int32>
            ObjectHolder.Create(1234us)               .GetValue().GetType() === typeof<uint16>
            ObjectHolder.Create(1234s)                .GetValue().GetType() === typeof<int16>
            ObjectHolder.Create(false)                .GetValue().GetType() === typeof<bool>
            ObjectHolder.Create('a')                  .GetValue().GetType() === typeof<char>
            ObjectHolder.Create(255uy)                .GetValue().GetType() === typeof<byte>


            let un64 = ObjectHolder.Create(1234567890123456789UL)
            let v = un64.GetValue()
            let t = v.GetType()

            let un64 = ObjectHolder.Create(9234567890123456789UL)
            let v = un64.GetValue()
            let t = v.GetType()

            let strUn64 = JsonConvert.SerializeObject(un64)
            let un64_ = JsonConvert.DeserializeObject<ObjectHolder>(strUn64)
            let v_ = un64_.GetValue()

            /// v 를 serialize, deserialize 되었을 때의 type 이 원래 v 의 type 과 일치하는지 검사
            let checkSerializedType v (t:System.Type) =
                ObjectHolder.Create(v)
                |> JsonConvert.SerializeObject
                |> JsonConvert.DeserializeObject<ObjectHolder>
                |> (fun x -> x.GetValue().GetType())
                 === t

            checkSerializedType 1234567890123456789UL typeof<uint64>
            checkSerializedType 9234567890123456789UL typeof<uint64>
            checkSerializedType 1234149234567890123456789M typeof<Decimal>
            checkSerializedType false typeof<bool>
            checkSerializedType 'A' typeof<char>
            checkSerializedType "Hello" typeof<string>
            checkSerializedType 255uy typeof<byte>
            checkSerializedType 255s typeof<int16>
            checkSerializedType 255us typeof<uint16>
            checkSerializedType 1234567890 typeof<int32>
            checkSerializedType 1234567890u typeof<uint32>
            checkSerializedType 12345678901234L typeof<int64>
            checkSerializedType 3.14 typeof<double>
            checkSerializedType 3.14f typeof<single>

            /// v 를 serialize, deserialize 되었을 때의 원래 v 의 값과 일치하는지 검사
            let checkSerializedValue v (t:System.Type) =
                ObjectHolder.Create(v)
                |> JsonConvert.SerializeObject
                |> JsonConvert.DeserializeObject<ObjectHolder>
                |> (fun x -> x.GetValue())
                 === v

            checkSerializedValue 1234567890123456789UL typeof<uint64>
            checkSerializedValue 9234567890123456789UL typeof<uint64>
            //checkSerializedValue 1234149234567890123456789M typeof<Decimal>     // 1234149234567890000000000M
            checkSerializedValue false typeof<bool>
            checkSerializedValue 'A' typeof<char>
            checkSerializedValue "Hello" typeof<string>
            checkSerializedValue 255uy typeof<byte>
            checkSerializedValue 255s typeof<int16>
            checkSerializedValue 255us typeof<uint16>
            checkSerializedValue 1234567890 typeof<int32>
            checkSerializedValue 1234567890u typeof<uint32>
            checkSerializedValue 12345678901234L typeof<int64>
            checkSerializedValue 3.14 typeof<double>
            checkSerializedValue 3.14f typeof<single>



            let serialType = JsonConvert.SerializeObject >> JsonConvert.DeserializeObject<ObjectHolder> >> (fun x -> x.GetValue().GetType())
            ObjectHolder.Create(1234567890123456789UL)
            |> JsonConvert.SerializeObject
            |> JsonConvert.DeserializeObject<ObjectHolder>
            |> (fun x -> x.GetValue().GetType())
             === typeof<uint64>