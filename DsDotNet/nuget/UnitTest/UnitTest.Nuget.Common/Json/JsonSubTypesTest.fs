namespace T

open NUnit.Framework
open Newtonsoft.Json
open JsonSubTypes

open Dual.Common.UnitTest.FS
open Dual.Common.Core.FS


[<AutoOpen>]
module rec TypePropName =
    [<JsonConverter(typedefof<JsonSubtypes>, "JsonType")>]
    type IJsonTyped =
        abstract member JsonType: string with get

    type Dog() =
        interface IJsonTyped with member x.JsonType = x.JsonType
        member val JsonType = "Dog"
        member val Breed = "" with get, set


    type Cat() =
        interface IJsonTyped with member x.JsonType = x.JsonType
        member val JsonType = "Cat"
        member val Declawed = false with get, set


[<AutoOpen>]
module JsonSubTypesCommon =
    let dogJson = "{\"JsonType\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}"
    let catJson = "{\"JsonType\":\"Cat\",\"Declawed\":true}"
    let dog = JsonConvert.DeserializeObject<IJsonTyped>(dogJson);
    let cat = JsonConvert.DeserializeObject<IJsonTyped>(catJson);
    let dogJson2 = JsonConvert.SerializeObject(dog)
    let catJson2 = JsonConvert.SerializeObject(cat)


module JsonSubTypesTest =
    [<TestFixture>]
    type SerializeTest() =
        [<Test>]
        member _.``Polymorphic`` () =
            dogJson === dogJson2
            catJson === catJson2
            dog.GetType().Name === "Dog"
            cat.GetType().Name === "Cat"

            let animalJson = $"[{dogJson}, {catJson}]"
            let animals = JsonConvert.DeserializeObject<IJsonTyped[]>(animalJson);
            animals[0].GetType().Name === "Dog"
            animals[1].GetType().Name === "Cat"
            ()

        [<Test>]
        member _.``Object Holder Test`` () =
            let a = ObjectHolder.CreateFromObject(24u)
            let b = ObjectHolder.CreateFromObject("Hello")
            let c = ObjectHolder.CreateFromObject(3.14)
            let ja = JsonConvert.SerializeObject(a)
            let jb = JsonConvert.SerializeObject(b)
            let jc = JsonConvert.SerializeObject(c)
            let aa = JsonConvert.DeserializeObject<ObjectHolder>(ja)
            let bb = JsonConvert.DeserializeObject<ObjectHolder>(jb)
            let cc = JsonConvert.DeserializeObject<ObjectHolder>(jc)
            ()



module XJsonSubTypesGenericTest =

    type Generic<'T>(content:'T) =
        interface IJsonTyped with
            member _.JsonType = typeof<'T>.Name
        member val Content:'T = content


    [<TestFixture>]
    type GenericTest() =
        // ObjectHolder class 사용 방안 참고
        [<Test>]
        member _.``X Generic`` () =
            let genericDog = Generic<IJsonTyped>(dog)
            let genericCat = Generic<IJsonTyped>(cat)
            let xxx1 = genericDog.GetType().Name
            let xxx2 = genericCat.GetType().Name
            let yyy = xxx1
            let zzz = xxx2
            let genericJson = "{\"JsonType\":\"Generic<int>\",\"Content\":42}"
            let generic = JsonConvert.DeserializeObject<IJsonTyped>(genericJson);
            generic.GetType().Name === "Int32"
            ()

