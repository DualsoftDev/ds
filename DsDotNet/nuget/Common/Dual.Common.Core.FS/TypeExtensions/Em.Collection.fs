namespace Dual.Common.Core.FS

open System
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices


[<AutoOpen>]
module EnumuerableExt =
    (************* Net9.0 부터 지원되지 않는 형식 *************)
    // instead Seq<'T>
    type IEnumerable<'T> with
        member x.isEmpty() = Seq.isEmpty x
        member x.length() = Seq.length x
        member x.any() = Seq.isEmpty x |> not
        member x.any f = Seq.tryFind f x |> Option.isSome
        member x.realize() = Array.ofSeq x |> ignore
    (************* Net9.0 부터 지원되지 않는 형식 *************)

    let groupByToDictionary<'V, 'K when 'K: equality> (keySelector:'V->'K) (xs:'V seq) =
        xs.GroupBy(keySelector)
            .Select(fun g -> g.Key, g.ToArray())
            |> dict
            |> Dictionary

    /// Seq.exactlyOne 와 동일
    let exactlyOne = Seq.exactlyOne


type SeqExt =
    /// test if sequence contains the value
    //[<Extension>] static member Contains(xs:'T seq, x:'T) = xs |> Seq.contains x  //System.Collections.Generic 혼동
    [<Extension>] static member ContainsAnyOf(xs:'T seq, x:'T seq) = xs |> Seq.containsAnyOf x
    [<Extension>] static member ContainsAllOf(xs:'T seq, x:'T seq) = xs |> Seq.containsAllOf x
    [<Extension>] static member AllEqual(xs:'T seq, x:'T) = xs |> Seq.forall((=) x)
    //[<Extension>] static member Any(xs:'a seq) = not <| Seq.isEmpty xs   //System.Collections.Generic 혼동
    [<Extension>] static member GetLength(xs:'a seq) = Seq.length xs
    [<Extension>] static member IsEmpty(xs:'a seq) = Seq.isEmpty xs
    [<Extension>] static member IsOneOf(x:'x, [<ParamArray>] (xs:'x array)) = xs |> Seq.contains x
    [<Extension>] static member IsNullOrEmpty(xs:'a seq) = xs = null || Seq.isEmpty xs
    [<Extension>] static member NonNullAny(xs:'a seq)    = xs <> null && xs.Any()
    [<Extension>] static member TapWhole(xs:'a seq, f)   = f xs; xs
    [<Extension>] static member TapInner(xs:'a seq, f)   = Seq.iter f xs; xs
    [<Extension>] static member Tap(xs:'a seq, f)        = Seq.iter f xs; xs
    /// Seq 요소 각각에 대한 tee 적용
    [<Extension>] static member Tee(xs:'a seq, f)        = Seq.iter f xs; xs

    [<Extension>] static member GroupByToDictionary<'V, 'K when 'K: equality>(xs:'V seq, keySelector:'V->'K) = groupByToDictionary keySelector xs
    [<Extension>] static member ToFSharpList(xs:'a seq)  = List.ofSeq xs
    [<Extension>] static member ToArray(xs:'a seq)      = Array.ofSeq xs
    /// Seq.ToArray().  Name space 충돌 회피용
    [<Extension>] static member ToArrayFs(xs:'a seq)    = Array.ofSeq xs
    [<Extension>] static member ToEnumerable<'a>(xs:System.Collections.IEnumerable) = seq { for x in xs -> x :?> 'a }
    [<Extension>] static member ToResizeArray(xs:'a seq) = xs |> ResizeArray

    [<Extension>] static member Repeat(xs:'a seq) = seq {while true do yield! xs}

    (*  List.fold (+) 0 [1; 2; 3] = ((0 + 1) + 2) + 3
        List.foldBack (+) [1; 2; 3] 0 = 1 + (2 + (3 + 0))   *)
    /// Seq.fold 와 동일
    ///
    /// [x] -> (acc -> x -> acc) -> acc -> acc
    [<Extension>] static member FoldLeft(xs:'a seq, f, seed)   = Seq.fold f seed xs
    /// Seq.foldBack 와 동일
    ///
    /// [x] -> (x -> acc -> acc) -> acc -> acc
    [<Extension>] static member FoldRight(xs:'a seq, f, seed)  = Seq.foldBack f xs seed


    /// Seq.allPairs 와 동일
    [<Extension>] static member AllPairs(xs:'x seq, ys:'y seq)    = Seq.allPairs xs ys
    /// Seq.cache 와 동일
    [<Extension>] static member Cache(xs:'a seq)                  = Seq.cache xs
    /// Seq.choose 와 동일
    [<Extension>] static member Choose(xs:'a seq, f)              = Seq.choose f xs
    /// Seq.chunkBySize 와 동일
    [<Extension>] static member ChunkBySize(xs:'a seq, chunkSize) = Seq.chunkBySize chunkSize xs
    /// Seq.collect 와 동일.  (>>=)
    [<Extension>] static member Collect(xs:'a seq, f)             = Seq.collect f xs
    /// Seq.collect 와 동일.  (>>=)
    [<Extension>] static member Bind(xs:'a seq, f)                = Seq.collect f xs
    /// Seq.compareWith 와 동일
    [<Extension>] static member CompareWith(xs1:'x seq, xs2:'x seq, f)    = Seq.compareWith f xs1 xs2
    /// Seq.exactlyOne 와 동일
    [<Extension>] static member ExactlyOne(xs:'a seq)    = Seq.exactlyOne xs
    /// Seq.filter 와 동일
    [<Extension>] static member Filter(xs:'a seq, f)     = Seq.filter f xs
    /// Seq.find 와 동일
    [<Extension>] static member Find(xs:'a seq, f)       = Seq.find f xs
    /// Seq.tryHead 와 동일
    [<Extension>] static member Fold(xs:'a seq, f, seed) = Seq.fold f seed xs
    /// Seq.foldBack 와 동일
    [<Extension>] static member FoldBack(xs:'a seq, f, seed)  = Seq.foldBack f xs seed
    /// Seq.forall 와 동일
    [<Extension>] static member ForAll(xs:'a seq, f)     = Seq.forall f xs
    /// Seq.iter 와 동일
    [<Extension>] static member ForEach(xs:'a seq, f)    = Seq.iter f xs
    /// Seq.head 와 동일
    [<Extension>] static member Head(xs:'a seq)          = Seq.head xs
    /// Seq.indexed 와 동일
    [<Extension>] static member Indexed(xs:'a seq)       = Seq.indexed xs
    /// Seq.iter 와 동일.  (>>:)
    [<Extension>] static member Iter(xs:'a seq, f)       = Seq.iter f xs
    /// Seq.map 와 동일.  (>>-)
    [<Extension>] static member Map(xs:'a seq, f)        = Seq.map f xs
    /// Seq.pairwise 와 동일
    [<Extension>] static member Pairwise(xs:'a seq)      = Seq.pairwise xs
    /// Seq.permute 와 동일
    [<Extension>] static member Permute(xs:'a seq, indexMap) = Seq.permute indexMap xs
    /// Seq.pick 와 동일
    [<Extension>] static member Pick(xs:'a seq, chooser) = Seq.pick chooser xs
    /// Seq.readonly 와 동일
    [<Extension>] static member ReadOnly(xs:'a seq)      = Seq.readonly xs
    /// Seq.reduce 와 동일
    [<Extension>] static member Reduce(xs:'a seq, f)     = Seq.reduce f xs
    /// Seq.reduceBack 와 동일
    [<Extension>] static member ReduceBack(xs:'a seq, f) = Seq.reduceBack f xs
    /// Seq.removeAt 와 동일
    [<Extension>] static member RemoveAt(xs:'a seq, index) = Seq.removeAt index xs
    /// Seq.removeManyAt 와 동일
    [<Extension>] static member RemoveManyAt(xs:'a seq, index, count) = Seq.removeManyAt index count xs
    /// Seq.scan 와 동일
    [<Extension>] static member Scan(xs:'x seq, folder:'z->'x->'z, z:'z)      = Seq.scan folder z xs
    /// Seq.scanBack 와 동일
    [<Extension>] static member ScanBack(xs:'x seq, folder:'x->'z->'z, z:'z)  = Seq.scanBack folder xs z
    /// Seq.singleton 와 동일
    [<Extension>] static member Singleton(x)                   = Seq.singleton x
    /// Seq.splitInto 와 동일
    [<Extension>] static member SplitInto(xs:'a seq, count)    = Seq.splitInto count xs
    /// Seq.transpose 와 동일
    [<Extension>] static member Transpose(xss:seq<#seq<'a>>)   = Seq.transpose xss
    /// Seq.tryExactlyOne 와 동일
    [<Extension>] static member TryExactlyOne(xs:'a seq)       = Seq.tryExactlyOne xs
    /// Seq.tryFind 와 동일
    [<Extension>] static member TryFind(xs:'a seq, f)          = Seq.tryFind f xs
    /// Seq.tryFindBack 와 동일
    [<Extension>] static member TryFindBack(xs:'a seq, f)      = Seq.tryFindBack f xs
    /// Seq.tryFindIndex 와 동일
    [<Extension>] static member TryFindIndex(xs:'a seq, f)     = Seq.tryFindIndex f xs
    /// Seq.tryFindIndexBack 와 동일
    [<Extension>] static member TryFindIndexBack(xs:'a seq, f) = Seq.tryFindIndexBack f xs
    /// Seq.tryHead 와 동일
    [<Extension>] static member TryHead(xs:'a seq)             = Seq.tryHead xs
    /// Seq.tryItem 와 동일
    [<Extension>] static member TryItem(xs:'a seq, index)      = Seq.tryItem index xs
    /// Seq.tryLast 와 동일
    [<Extension>] static member TryLast(xs:'a seq)             = Seq.tryLast xs
    /// Seq.tryPick 와 동일
    [<Extension>] static member TryPick(xs:'a seq, chooser)    = Seq.tryPick chooser xs
    /// Seq.updateAt 와 동일
    [<Extension>] static member UpdateAt(xs:'a seq, index, value) = Seq.updateAt index value xs
    /// Seq.windowed 와 동일
    [<Extension>] static member Windowed(xs:'a seq, windowSize)   = Seq.windowed windowSize xs
    /// Seq.zip 와 동일
    [<Extension>] static member Zip(xs:'x seq, ys:'y seq)         = Seq.zip xs ys
    /// Seq.zip3 와 동일
    [<Extension>] static member Zip3(xs:'x seq, ys:'y seq, zs:'z seq) = Seq.zip3 xs ys zs


[<AutoOpen>]
module DotNetCollectionExt =
    (* C# list extension *)
    type System.Collections.Generic.List<'t> with
        member xs.AddIfNotContains(x:'t) =
            if not (xs.Contains x) then
                xs.Add x

    type IDictionary<'k, 'v> with
        /// Dictionary 에 key 가 존재하면 Some(value) 반환, 없으면 None 반환
        ///
        /// - 기존 TryFind 에서 이름 변경.  SeqExt.TryFind 와 이름 중복됨.
        ///
        /// - Dicitionary.TryGetValue(key, out value) -> bool return 하고, true 인 경우에 값 확인 가능
        ///
        /// - TryFindValue(key) -> Option 값 반환
        member xs.TryFindValue(key:'k) =
            match xs.TryGetValue(key) with
            | true, value -> Some value
            | _ -> None
        member xs.TryFindIt = xs.TryFindValue


    type Dictionary<'k, 'v> with
        /// 없어서 add 성공시 true, 있어서 update 시 false 반환
        member xs.AddOrReplace(key:'k, value:'v) =
            if xs.ContainsKey key then
                xs.[key] <- value
                false
            else
                xs.Add(key, value)
                true
        member xs.TryAdd(key:'k, value:'v) =
            if xs.ContainsKey key then
                false
            else
                xs.[key] <- value
                true

        /// Dictionary 의 TryGetValue 반환값 bool * 'v 를 Option<'v> 로 변환해서 반환
        member xs.TryGet(key:'k) =
            match xs.TryGetValue(key) with
            | true, value -> Some value
            | _ -> None

    type HashSet<'k> with
        /// Dictionary 의 TryGetValue 반환값 bool * 'v 를 Option<'v> 로 변환해서 반환
        member xs.TryGet(key:'k) =
            xs.Contains key ?= (Some key, None)

//open System.Collections.Generic
//let (+=) (dic: Dictionary<'k, 'v>) (key:'k, value:'v) =
//    if dic.ContainsKey(key) then
//        failwith $"Dictionary already contains value {value} for key {key}"

//    dic.Add(key, value)
//let (+=) (dic: Dictionary<'k, 'v>) (KeyValuePair(k, v)) = dic += (k, v)


    type HashSet<'k> with
        member xs.AddRange(keys:'k seq) =
            [ for k in keys do
                xs.Add k ] |> List.forall id

    type List<'T> with
        member x.IsEmpty() = x |> Seq.isEmpty


