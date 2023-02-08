namespace Engine.Common.FS

open System
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices


[<AutoOpen>]
module EnumuerableExt =
    // instead Seq<'T>
    type IEnumerable<'T> with
        member x.isEmpty() = Seq.isEmpty x
        member x.length() = Seq.length x
        member x.any() = Seq.isEmpty x |> not
        member x.any f = Seq.tryFind f x |> Option.isSome
        member x.realize() = Array.ofSeq x |> ignore

    let groupByToDictionary<'V, 'K when 'K: equality>(xs:'V seq) (keySelector:'V->'K) =
        xs.GroupBy(keySelector)
            .Select(fun g -> g.Key, g.ToArray())
            |> dict
            |> Dictionary

[<Extension>] // type SeqExt =
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

    [<Extension>] static member GroupByToDictionary<'V, 'K when 'K: equality>(xs:'V seq, keySelector:'V->'K) = groupByToDictionary xs keySelector
    [<Extension>] static member ToFSharpList(xs:'a seq)  = List.ofSeq xs
    [<Extension>] static member ToArray(xs:'a seq)      = Array.ofSeq xs
    [<Extension>] static member ToEnumerable<'a>(xs:System.Collections.IEnumerable) = seq { for x in xs -> x :?> 'a }
    [<Extension>] static member ToResizeArray(xs:'a seq) = xs |> ResizeArray

    [<Extension>] static member Repeat(xs:'a seq) = seq {while true do yield! xs}

    (*  List.fold (+) 0 [1; 2; 3] = ((0 + 1) + 2) + 3
        List.foldBack (+) [1; 2; 3] 0 = 1 + (2 + (3 + 0))   *)
    /// [x] -> (acc -> x -> acc) -> acc -> acc
    [<Extension>] static member FoldLeft(xs:'a seq, f, seed)   = Seq.fold f seed xs
    /// [x] -> (x -> acc -> acc) -> acc -> acc
    [<Extension>] static member FoldRight(xs:'a seq, f, seed)  = Seq.foldBack f xs seed


    [<Extension>] static member AllPairs(xs:'x seq, ys:'y seq)    = Seq.allPairs xs ys
    [<Extension>] static member Cache(xs:'a seq)                  = Seq.cache xs
    [<Extension>] static member Choose(xs:'a seq, f)              = Seq.choose f xs
    [<Extension>] static member ChunkBySize(xs:'a seq, chunkSize) = Seq.chunkBySize chunkSize xs
    [<Extension>] static member Collect(xs:'a seq, f)             = Seq.collect f xs
    [<Extension>] static member CompareWith(xs1:'x seq, xs2:'x seq, f)    = Seq.compareWith f xs1 xs2
    [<Extension>] static member ExactlyOne(xs:'a seq)    = Seq.exactlyOne xs
    [<Extension>] static member Filter(xs:'a seq, f)     = Seq.filter f xs
    [<Extension>] static member Find(xs:'a seq, f)       = Seq.find f xs
    [<Extension>] static member Fold(xs:'a seq, f, seed) = Seq.fold f seed xs
    [<Extension>] static member FoldBack(xs:'a seq, f, seed)  = Seq.foldBack f xs seed
    [<Extension>] static member ForAll(xs:'a seq, f)     = Seq.forall f xs
    [<Extension>] static member ForEach(xs:'a seq, f)    = Seq.iter f xs
    [<Extension>] static member Head(xs:'a seq)          = Seq.head xs
    [<Extension>] static member Indexed(xs:'a seq)       = Seq.indexed xs
    [<Extension>] static member Iter(xs:'a seq, f)       = Seq.iter f xs
    [<Extension>] static member Map(xs:'a seq, f)        = Seq.map f xs
    [<Extension>] static member Pairwise(xs:'a seq)      = Seq.pairwise xs
    [<Extension>] static member Permute(xs:'a seq, indexMap) = Seq.permute indexMap xs
    [<Extension>] static member Pick(xs:'a seq, chooser) = Seq.pick chooser xs
    [<Extension>] static member ReadOnly(xs:'a seq)      = Seq.readonly xs
    [<Extension>] static member Reduce(xs:'a seq, f)     = Seq.reduce f xs
    [<Extension>] static member ReduceBack(xs:'a seq, f) = Seq.reduceBack f xs
    [<Extension>] static member RemoveAt(xs:'a seq, index) = Seq.removeAt index xs
    [<Extension>] static member RemoveManyAt(xs:'a seq, index, count) = Seq.removeManyAt index count xs
    [<Extension>] static member Scan(xs:'x seq, folder:'z->'x->'z, z:'z)      = Seq.scan folder z xs
    [<Extension>] static member ScanBack(xs:'x seq, folder:'x->'z->'z, z:'z)  = Seq.scanBack folder xs z
    [<Extension>] static member Singleton(x)                   = Seq.singleton x
    [<Extension>] static member SplitInto(xs:'a seq, count)    = Seq.splitInto count xs
    [<Extension>] static member Transpose(xss:seq<#seq<'a>>)   = Seq.transpose xss
    [<Extension>] static member TryExactlyOne(xs:'a seq)       = Seq.tryExactlyOne xs
    [<Extension>] static member TryFind(xs:'a seq, f)          = Seq.tryFind f xs
    [<Extension>] static member TryFindBack(xs:'a seq, f)      = Seq.tryFindBack f xs
    [<Extension>] static member TryFindIndex(xs:'a seq, f)     = Seq.tryFindIndex f xs
    [<Extension>] static member TryFindIndexBack(xs:'a seq, f) = Seq.tryFindIndexBack f xs
    [<Extension>] static member TryHead(xs:'a seq)             = Seq.tryHead xs
    [<Extension>] static member TryItem(xs:'a seq, index)      = Seq.tryItem index xs
    [<Extension>] static member TryLast(xs:'a seq)             = Seq.tryLast xs
    [<Extension>] static member TryPick(xs:'a seq, chooser)    = Seq.tryPick chooser xs
    [<Extension>] static member UpdateAt(xs:'a seq, index, value) = Seq.updateAt index value xs
    [<Extension>] static member Windowed(xs:'a seq, windowSize)   = Seq.windowed windowSize xs
    [<Extension>] static member Zip(xs:'x seq, ys:'y seq)         = Seq.zip xs ys
    [<Extension>] static member Zip3(xs:'x seq, ys:'y seq, zs:'z seq) = Seq.zip3 xs ys zs


[<AutoOpen>]
module DotNetCollectionExt =
    (* C# list extension *)
    type System.Collections.Generic.List<'t> with
        member xs.AddIfNotContains(x:'t) =
            if not (xs.Contains x) then
                xs.Add x

    type Dictionary<'k, 'v> with
        /// Dictionary 에 key 가 존재하면 Some(value) 반환, 없으면 None 반환
        member xs.TryFind(key:'k) =
            if xs.ContainsKey key then
                Some xs.[key]
            else
                None
        member xs.TryFindIt = xs.TryFind
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

    type HashSet<'k> with
        member xs.AddRange(keys:'k seq) =
            [ for k in keys do
                xs.Add k ] |> List.forall id

    type List<'T> with
        member x.IsEmpty() = x |> Seq.isEmpty


