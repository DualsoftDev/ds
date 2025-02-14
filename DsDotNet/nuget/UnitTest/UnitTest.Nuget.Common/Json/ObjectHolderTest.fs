namespace T

open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open NUnit.Framework
open System
open Newtonsoft.Json
open Dual.Common.Base.FS
open Newtonsoft.Json.Linq


[<AutoOpen>]
module ObjectHolderTestModule =

    type NaiveHolder(value:obj) =
        new() = NaiveHolder(null)
        member val Value = value with get, set

    type Container(holders:ObjectHolder seq) =
        member val Holders = holders.ToArray() with get, set

    [<TestFixture>]
    type ObjectHolderTest() =

        [<Test>]
        member _.DefaultSerializeTest() =
            //let x = ObjectHolder()

            // System.Text.Json 은 F# discriminated union serialization 을 지원하지 않는다.
            (fun () -> System.Text.Json.JsonSerializer.Serialize(ObjectHolderType.Undefined) |> ignore)
                |> ShouldFailWithSubstringT "F# discriminated union serialization is not supported"

            let un64 = NaiveHolder(1234567890UL)
            let strUn64 = EmJson.ToJson un64
            let un64_ = EmJson.FromJson<NaiveHolder>(strUn64)
            un64.Value.GetType().Name === "UInt64"
            un64_.Value.GetType().Name === "Int64"
            ()


            let strUn64 = System.Text.Json.JsonSerializer.Serialize(un64)
            let un64_ = System.Text.Json.JsonSerializer.Deserialize<NaiveHolder>(strUn64)
            un64_.Value.GetType().Name === "JsonElement"    // ???
            ()

        [<Obsolete("Fix me")>]
        [<Test>]
        member _.ObjectHolderLaterAssignValueTest() =
            let holder = ObjectHolder.Create(ObjectHolderType.UInt32, null)
            let json2 = EmJson.ToJson holder
            let holder2 = EmJson.FromJson<ObjectHolder> json2
            holder2.Value === null
            holder2.Type === ObjectHolderType.UInt32

            holder.Value <- 1234567890u
            let json3 = EmJson.ToJson holder
            let holder3 = EmJson.FromJson<ObjectHolder> json3
            holder3.Type === ObjectHolderType.UInt32

            //(fun () -> holder.Value <- 1234567890) |> ShouldFailWithSubstringT "Type mismatch"

            ()

        [<Test>]
        member _.ObjectHolderUInt64Test() =
            let h = ObjectHolder.CreateFromObject(1234567890123456789UL)
            h.GetValue() === 1234567890123456789UL
            let json = EmJson.ToJson h
            let h2 = EmJson.FromJson<ObjectHolder> json
            h2.GetValue() === 1234567890123456789UL
            ()

        [<Test>]
        member _.ObjectHolderArrayTest() =
            let holders = [
                ObjectHolder.CreateFromObject(1234567890123456789UL)
                ObjectHolder.CreateFromObject(123456789L)
                ObjectHolder.CreateFromObject(123456789u)
                ObjectHolder.CreateFromObject(123456789)
                ObjectHolder.CreateFromObject(1234us)
                ObjectHolder.CreateFromObject(1234s)
                ObjectHolder.CreateFromObject(false)
                ObjectHolder.CreateFromObject('a')
                ObjectHolder.CreateFromObject(255uy)
            ]
            let container = Container(holders)
            let json = EmJson.ToJson container

            let container2 = EmJson.FromJson<Container> json
            let h = container2.Holders
            h[0].Type === ObjectHolderType.UInt64
            h[1].Type === ObjectHolderType.Int64
            h[2].Type === ObjectHolderType.UInt32
            h[3].Type === ObjectHolderType.Int32
            h[4].Type === ObjectHolderType.UInt16
            h[5].Type === ObjectHolderType.Int16
            h[6].Type === ObjectHolderType.Bool
            h[7].Type === ObjectHolderType.Char
            h[8].Type === ObjectHolderType.Byte

            h[0].Value === 1234567890123456789UL
            h[1].Value === 123456789L
            h[2].Value === 123456789u
            h[3].Value === 123456789
            h[4].Value === 1234us
            h[5].Value === 1234s
            h[6].Value === false
            h[7].Value === 'a'
            h[8].Value === 255uy

            ()


        [<Test>]
        member _.ObjectHolderSerializeTest() =
            let serializeTest v =
                let holder = ObjectHolder.CreateFromObject(v)
                let str1 = EmJson.ToJson holder
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

            ObjectHolder.CreateFromObject(1234567890123456789UL).GetValue().GetType() === typeof<uint64>
            ObjectHolder.CreateFromObject(123456789L)           .GetValue().GetType() === typeof<int64>
            ObjectHolder.CreateFromObject(123456789u)           .GetValue().GetType() === typeof<uint32>
            ObjectHolder.CreateFromObject(123456789)            .GetValue().GetType() === typeof<int32>
            ObjectHolder.CreateFromObject(1234us)               .GetValue().GetType() === typeof<uint16>
            ObjectHolder.CreateFromObject(1234s)                .GetValue().GetType() === typeof<int16>
            ObjectHolder.CreateFromObject(false)                .GetValue().GetType() === typeof<bool>
            ObjectHolder.CreateFromObject('a')                  .GetValue().GetType() === typeof<char>
            ObjectHolder.CreateFromObject(255uy)                .GetValue().GetType() === typeof<byte>


            let un64 = ObjectHolder.CreateFromObject(1234567890123456789UL)
            let v = un64.GetValue()
            let t = v.GetType()

            let un64 = ObjectHolder.CreateFromObject(9234567890123456789UL)
            let v = un64.GetValue()
            let t = v.GetType()

            let strUn64 = JsonConvert.SerializeObject(un64)
            let un64_ = JsonConvert.DeserializeObject<ObjectHolder>(strUn64)
            let v_ = un64_.GetValue()

            /// v 를 serialize, deserialize 되었을 때의 type 이 원래 v 의 type 과 일치하는지 검사
            let checkSerializedType v (t:System.Type) =
                ObjectHolder.CreateFromObject(v)
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
                ObjectHolder.CreateFromObject(v)
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
            ObjectHolder.CreateFromObject(1234567890123456789UL)
            |> JsonConvert.SerializeObject
            |> JsonConvert.DeserializeObject<ObjectHolder>
            |> (fun x -> x.GetValue().GetType())
             === typeof<uint64>

        [<Test>]
        member _.ObjectHolderTypeTest() =
            ObjectHolder.Create(typedefof<uint64>,  null).Type.ToSystemType() === typeof<uint64>
            ObjectHolder.Create(typedefof<int64>,   null).Type.ToSystemType() === typeof<int64>
            ObjectHolder.Create(typedefof<uint32>,  null).Type.ToSystemType() === typeof<uint32>
            ObjectHolder.Create(typedefof<int32>,   null).Type.ToSystemType() === typeof<int32>
            ObjectHolder.Create(typedefof<uint16>,  null).Type.ToSystemType() === typeof<uint16>
            ObjectHolder.Create(typedefof<int16>,   null).Type.ToSystemType() === typeof<int16>
            ObjectHolder.Create(typedefof<bool>,    null).Type.ToSystemType() === typeof<bool>
            ObjectHolder.Create(typedefof<char>,    null).Type.ToSystemType() === typeof<char>
            ObjectHolder.Create(typedefof<byte>,    null).Type.ToSystemType() === typeof<byte>




[<AutoOpen>]
module ObjectHolderTestModule2 =
    type TypeNameConverter() =
        inherit JsonConverter()

        let settings = JsonSerializerSettings(EmJson.DefaultSettings).Tee(fun s -> s.TypeNameHandling <- TypeNameHandling.Auto)

        override _.CanConvert(objectType) = true

        override _.WriteJson(writer, value, serializer) =
            let json = JsonConvert.SerializeObject(value, settings)
            writer.WriteRawValue(json)

        override _.ReadJson(reader, objectType, existingValue, serializer) =
            let token = JToken.Load(reader)
            JsonConvert.DeserializeObject(token.ToString(), objectType, settings)


    //[<AbstractClass>]
    type TypedAddress(address: string, typ:Type) =
        //interface IJsonTyped with member x.JsonType = x.JsonType
        //member val JsonType = "TypedAddress"

        [<JsonProperty(Order = -98)>] member val Address = address with get, set

        [<JsonConverter(typeof<TypeNameConverter>)>]
        [<JsonProperty(Order = -97)>] member val ObjectHolder = ObjectHolder.Create(typ, null) with get, set

    type InputParam(address: string, typ:Type, ?min:obj, ?max:obj) =
        inherit TypedAddress(address, typ)
        do
            assert(min.IsNone || min.Value.GetType() = typ)
            assert(max.IsNone || max.Value.GetType() = typ)
        let min = min |? null
        let max = max |? null

        [<JsonProperty>] member val Min = ObjectHolder.Create(typ, min) with get, set
        [<JsonProperty>] member val Max = ObjectHolder.Create(typ, max) with get, set

    type OutputParam(address: string, typ:Type, ?value:obj) =
        inherit TypedAddress(address, typ)
        do
            assert(value.IsNone || value.Value.GetType() = typ)

    type IOParam(input:InputParam, output:OutputParam) =
        member val Input:InputParam = input with get, set
        member val Output = output with get, set
        member val Others = ["Hello"; "World"] with get, set
    [<TestFixture>]
    type ObjectHolderTest() =

        [<Test>]
        member _.TestComplex() =
            let ta = TypedAddress("address1", typedefof<UInt32>)
            ta.ObjectHolder.Value <- 333u
            let json = EmJson.ToJson ta
            let ta2 = EmJson.FromJson<TypedAddress> json

            let param1 = InputParam("address1", typedefof<UInt32>, min = 10u, max = 100u)
            let param2 = OutputParam("address1", typedefof<UInt32>, value = 20u)

            let json = EmJson.ToJson param1
            let param11 = EmJson.FromJson<InputParam> json

            let ioParam = IOParam(param1, param2)


            let json = EmJson.ToJson(ioParam)
            let ioParam2 = EmJson.FromJson<IOParam> json
            ioParam.Input.Min.Value === ioParam2.Input.Min.Value
            ioParam.Output.ObjectHolder.Value === ioParam2.Output.ObjectHolder.Value

