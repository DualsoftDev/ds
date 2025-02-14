namespace Dual.Common.Core.FS

open System.Linq
open System.Collections.Generic

[<RequireQualifiedAccess>]
module Map =
    let keys   x = x |> Map.toSeq |> Seq.map fst
    let values x = x |> Map.toSeq |> Seq.map snd

[<AutoOpen>]
module MapModule =
    type Map<'k, 'v> when 'k: comparison with
        /// Map 에서 key 만 추출
        member x.Keys with get() = x |> Map.keys
        /// Map 에서 Values 만 추출
        member x.Values with get() = x |> Map.values
        /// Map 에서 주어진 key 존재 여부 검사
        member x.contains kvPredicate = x |> Map.exists kvPredicate
        member x.exists kvPredicate = x |> Map.exists kvPredicate

    /// 다중 value 허용 dictionary.  NOT thread safe
    type MultiMap<'k,'v when 'k: equality> private (tpls: seq<'k*('v seq)>) =
    //type MultiMap<'k,'v when 'k: equality> private (tpls: seq<'k* #seq<'v>>) =
        let dictionary =
            tpls
            |> Seq.map2nd HashSet
            |> dict
            |> Dictionary

        let addValues k (vs:'v seq) =
            match dictionary.TryGetValue k with
            | true, hash ->
                vs |> Seq.iter (fun v -> hash.Add v |> ignore)
            | _ ->
                let hash = HashSet(vs)
                dictionary.Add(k, hash)

        let add k v = addValues k [v]

        //new (tpls: ('k*'v) seq) =
        //    let kvsTpls =
        //        tpls
        //        |> Seq.groupBy fst // seq<'k * seq<'k * 'v>>
        //        |> Seq.map2nd (Seq.map snd) // seq<'k * seq<'v>>
        //    MultiMap(kvsTpls, 0)


        member x.Dictionary = dictionary


        //new (tpls: ('k*('v seq)) seq) =
        //    MultiMap(tpls, 0)

        // https://stackoverflow.com/questions/1785168/implementing-dictionary-subclass-in-f

        //static member CreateDeep(tpls: ('k*('v seq)) seq) = MultiMap(tpls)

        /// Key 에 대해서 다중 values 의 list 로 multimap 생성 : [ key - [value] ]
        static member CreateDeep(tpls: seq<'k*(seq<'v>)> ) = MultiMap(tpls)

        /// Key - value 리스트로부터 multimap 생성 : key 중복 시, 하나의 키를 만들고 value list 연결
        static member CreateFlat(tpls: ('k*'v) seq) =
            let kvsTpls =
                tpls
                |> Seq.groupBy fst // seq<'k * seq<'k * 'v>>
                |> Seq.map2nd (Seq.map snd) // seq<'k * seq<'v>>
            MultiMap(kvsTpls)


        static member CreateEmpty<'k, 'v>() =
            let tpl : ('k*'v) seq = Seq.empty
            MultiMap.CreateFlat(tpl)


        member x.Add(k, v:'v) = add k v
        member x.Set(k, vs:'v seq) = x.Item(k) <- vs
        member x.Add(k, vs:'v seq) = addValues k vs
        member x.Remove(k) = dictionary.Remove(k)
        member x.GetEnumerator() = dictionary.GetEnumerator()
        member x.TryGetValue(k, v) = dictionary.TryGetValue(k, ref v)
        member x.Clear() = dictionary.Clear()

        member x.ContainsKey(k) = dictionary.ContainsKey(k)
        member x.Contains(kvp:KeyValuePair<'k, HashSet<'v>>) = dictionary.TryGetValue(kvp.Key, ref kvp.Value)
        member x.ContainsKeyAndValue(k, v) = dictionary.ContainsKey(k) && dictionary.[k].Contains(v)
        member x.Count = dictionary.Count
        member x.Keys = dictionary.Keys
        member x.Values = dictionary.Values
        member x.FlatValues = x.Values |> Seq.collect id
        member x.Item
            with get k = dictionary.[k]
            and set k vs = dictionary.Item(k) <- HashSet vs

        /// [ Key * HashSet<Value> ] 형태로 반환
        member x.EnumerateKeyAndGroupValue() =
            dictionary |> Seq.map Tuple.ofKeyValuePair

        /// [ Key * Value ] 형태로 반환
        member x.EnumerateKeyAndValue() =
            x.EnumerateKeyAndGroupValue()
            |> Seq.map (fun (k, vs) -> vs |> Seq.map (fun v -> k, v))
            |> Seq.flatten

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Maps =
    /// Chained dictionary (다중 dictionary) 에서 항목 찾기
    let findAll key maps =
        maps
        |> Seq.map (Map.tryFind key)
        |> Seq.choose id

    let tryFind key maps =
        maps
        |> Seq.tryPick (Map.tryFind key)

    let find key maps =
        tryFind key maps
        |> Option.get

