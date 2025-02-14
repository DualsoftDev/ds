namespace Dual.Common.Core.FS

open System.Collections.Generic

/// 문자열을 key 로 하는 임의의 value 를 저장할 수 있는 dictionary
// see DynamicDictionaryTest @ CollectionTest.fs
type DynamicDictionary() =
    inherit Dictionary<string, obj>()

    new(dictionary: IDictionary<string, obj>) =
        DynamicDictionary() then        // 생성자 chaning
        for kv in dictionary do
            base.Add(kv.Key, kv.Value)

    new(seq: seq<string * obj>) as this =
        DynamicDictionary() then
        for (k, v) in seq do
            this.Add(k, v)

    member x.Set<'T>(v:'T) = x.Set(typeof<'T>.Name, v)

    member x.Set<'T>(k:string, v:'T) =
        if x.ContainsKey(k) then
            x[k] <- v
        else
            x.Add(k, v)

    member x.Remove<'T>() =
        let key = typeof<'T>.Name
        x.Remove(key) |> ignore

    member x.TryGet<'T>() : 'T option = x.TryGet<'T>(typeof<'T>.Name)
    member x.TryGet<'T>(key: string) : 'T option =
        match x.TryGetValue(key) with
        | (true, value) ->
            match value with
            | :? 'T as typedValue -> Some typedValue
            | _ -> None
        | _ -> None

    member x.TryGetBool  (key) = x.TryGet<bool>  (key)
    member x.TryGetInt   (key) = x.TryGet<int>   (key)
    member x.TryGetString(key) = x.TryGet<string>(key)
    member x.TryGetDouble(key) = x.TryGet<double>(key)

    /// 해당 Tag 를 찾지 못하면, 새로 생성해서 반환
    member x.ForceGet<'T> (creator:unit -> 'T): 'T =
        let createInstance() =
            creator() // 'T에 기본 생성자 제약
            |> tee(fun newInstance -> x.Set<'T>(newInstance))

        match x.TryGet(typeof<'T>.Name) with
        | Some t -> t
        | None -> createInstance()



    member x.Get<'T>  ()    = x.TryGet<'T>    ()    |> Option.get
    member x.Get<'T>  (key) = x.TryGet<'T>    (key) |> Option.get
    member x.GetBool  (key) = x.TryGet<bool>  (key) |> Option.get
    member x.GetInt   (key) = x.TryGet<int>   (key) |> Option.get
    member x.GetString(key) = x.TryGet<string>(key) |> Option.get
    member x.GetDouble(key) = x.TryGet<double>(key) |> Option.get
