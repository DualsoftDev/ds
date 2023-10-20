namespace T

open Dual.Common.Core
open Dual.UnitTest.Common.FS
open NUnit.Framework
open System
open System.Text.Json
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

            ObjectHolder.Create(1234567890123456789UL).GetValue().GetType() === typeof<uint64>
            ObjectHolder.Create(123456789L).GetValue().GetType() === typeof<int64>
            ObjectHolder.Create(123456789u).GetValue().GetType() === typeof<uint32>
            ObjectHolder.Create(123456789).GetValue().GetType() === typeof<int32>
            ObjectHolder.Create(1234us).GetValue().GetType() === typeof<uint16>
            ObjectHolder.Create(1234s).GetValue().GetType() === typeof<int16>
            ObjectHolder.Create(false).GetValue().GetType() === typeof<bool>
            ObjectHolder.Create('a').GetValue().GetType() === typeof<char>
            ObjectHolder.Create(255uy).GetValue().GetType() === typeof<byte>

            let un64 = ObjectHolder.Create(1234567890123456789UL)
            let v = un64.GetValue()
            let t = v.GetType()
            
            let strUn64 = JsonConvert.SerializeObject(un64)
            let un64_ = JsonConvert.DeserializeObject<ObjectHolder>(strUn64)
            let v_ = un64_.GetValue()


            //let x64 = 1234567890123456789L
            //let b64 = 1234567890123456789L |> box
            //let xxx = b64 |> unbox<int64> |> uint64

            //let nn64 = 1234567890L
            //let xx64 = uint64 nn64 

            let nnn = 1234567890123456789UL
            let un64 = ObjectHolder(nnn, ObjectHolderType.UInt64)
            let strUn64 = JsonConvert.SerializeObject(un64)
            let un64_ = JsonConvert.DeserializeObject<ObjectHolder>(strUn64)
            let t = un64_.GetValue().GetType()
            //t =!= typeof<int>
            //t === typeof<JsonElement>


            ()
