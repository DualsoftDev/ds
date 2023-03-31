namespace Engine.Common.FS

open System.Linq
open System.Collections.Generic
open System
open System.Runtime.CompilerServices
open System.Collections
open System.Collections.Generic

#nowarn "0064"

[<AutoOpen>]
module SeqPrelude =
    /// Seq.empty
    let sempty = Seq.empty



[<RequireQualifiedAccess>]
module Seq =
    /// 두 sequence 가 같은지 비교: Exact match
    let equal seq1 seq2 =
        Enumerable.SequenceEqual(seq1, seq2)
    /// 두 sequence 의 짧은 길이 만큼만 같은지 비교
    let equalShort seq1 seq2 =
        (seq1, seq2) ||> Seq.forall2 (=)

    /// Seq.collect 와 동일
    let bind = Seq.collect

    let takeUntil f xs = Seq.takeWhile (f >> not) xs
    /// Seq.collect id 적용 : {{xs}} -> {xs}
    let flatten xss = xss |> Seq.collect id

    let mapSome mapper (options:'a option seq) = options |> Seq.choose (Option.map mapper)
    let mapTuple (mapper1:'a->'c) (mapper2:'b->'d) (xs:('a*'b) seq) =
        xs |> Seq.map (fun (a, b) -> mapper1 a, mapper2 b)

    /// tuple seq 에서 tuple 의 첫 항목들만 mapping 수행
    ///
    /// [ (1, "a"); (2, "b") ] |> map1st ((+) 1) ==> [(2, "a"); (3, "b")]
    let map1st mapper = mapTuple mapper id

    /// tuple 의 seq 에 대해서 tuple 의 snd 만 mapping
    ///
    // [ ("a", 1); ("b", 2) ] |> map2nd ((+) 1) ==> [("a", 2); ("b", 3)]
    let map2nd mapper = mapTuple id mapper

    /// tuple 의 fst projection 후, mapper 적용
    let mapProject1st mapper (xs:('a*_) seq) = xs |> Seq.map (fst >> mapper)
    /// tuple 의 snd projection 후, mapper 적용
    let mapProject2nd mapper (xs:(_*'a) seq) = xs |> Seq.map (snd >> mapper)


    /// seq 의 seq 에 대해서 내부의 seq 를 mapping
    ///
    /// seq { seq {1..3}; seq {5..9}} |> Seq.mapInner ((+) 1) ==> [[2; 3; 4]; [6; 7; 8; 9; 10]]
    let mapInner (mapper:'x->'b) (xss:'x seq seq) =
        xss |> Seq.map (Seq.map mapper)

    /// seq 의 seq 에 대해서 내부의 seq 에 Seq.mapi 적용
    let mapiInner (mapper:int -> 'x->'b) (xss:'x list list) =
        xss |> Seq.map (Seq.mapi mapper)

    // http://www.fssnip.net/7Rr/title/Enumerating-function
    /// Creates a function from a sequence that, when called, returns the items in the sequence
    let tryEnumerate (xs: seq<_>) =
        use en = xs.GetEnumerator()
        fun () ->
            match en.MoveNext() with
            | true -> Some en.Current
            | false -> None

    /// Creates a function from a sequence that, when called, returns the items in the sequence
    let enumerate (xs: seq<_>)  = tryEnumerate xs >> Option.get

    let orElse ys xs = if Seq.isEmpty xs then ys else xs
    let orElseWith f xs = if Seq.isEmpty xs then f() else xs

    // http://www.fssnip.net/18/title/Haskell-function-iterate
    /// seed 에 f 를 0번, 1번, ... 무한번 적용한 sequence 반환
    /// Seq.take 10 (iterate ((*)2) 1)
    /// =>  seq [1; 2; 4; 8; ...]
    let rec iterate f seed = seq {
      yield seed
      yield! iterate f (f seed) }

    /// 주어진 sequence 에서 조건을 만족하는 갯수를 반환
    let count predicate xs =
        xs |> Seq.filter predicate |> Seq.length

    // http://codebetter.com/matthewpodwysocki/2008/11/26/object-oriented-f-encapsulation-with-object-expressions/
    /// counter() 호출시마다 증가된  count 값을 반환하는 counter 함수를 만들어서 반환
    let counterGenerator start =
        let xs = iterate successor start |> Seq.cache
        enumerate xs

    let incrementalKeywordGenerator prefix start =
        let xs = iterate successor start |> Seq.map (sprintf "%s%d" prefix) |>  Seq.cache
        enumerate xs


    // http://www.fssnip.net/7WT/title/SeqtrySkip
    /// Tries to skip the given number of items and returns the rest. Disposes the enumerator at the end.
    /// skip 할 갯수가 seq 의 갯수보다 더 크면 Seq.skip 은 exception 발생하지만, trySkip 은 exception safe
    let trySkip count (xs: 'x seq) =
        seq {
            use e = xs.GetEnumerator ()
            let mutable i = 0
            let mutable dataAvailable = e.MoveNext ()
            while dataAvailable && i < count do
                dataAvailable <- e.MoveNext ()
                i <- i + 1
            if dataAvailable then
                yield e.Current
                while e.MoveNext () do
                    yield e.Current
        }

    /// System.Reactive 의 Observable.Window 를 참고해서 count 와 skip 을 구현
    /// Sliding / Hopping window 지원
    /// List.windowed 함수의 경우, skip 이 1 로 고정된 형태이고, count 갯수가 동일한 chunk 까지만 모으는데 반해,
    /// windowed2 는 count 와 skip 을 받고, 맨 마지막 chunk 는 count 보다 작은 갯수를 허용한다.
    /// Rx.NET in action.pdf, pp227
    let rec windowed2 count skip (xs:'x seq) =
        if xs |> Seq.isEmpty then
            Seq.empty
        else
            seq {
                yield xs |> Seq.truncate count
                yield! xs |> trySkip skip |> windowed2 count skip
            }

    [<Obsolete>]
    let counterGeneratorObsolete start =
        let count = ref start
        let counter()  =
            let key = !count
            incr count
            key
        counter

    [<Obsolete>]
    let incrementalKeywordGeneratorObsolete prefix start =
        let count = ref start
        let generator()  =
            let key = sprintf "%s%d" prefix !count
            incr count
            key
        generator


    module private TestMe0 =
        let a = counterGenerator 3
        assert(a() = 3)
        assert(a() = 4)
        let b = counterGenerator 0
        assert(b() = 0)
        assert(b() = 1)
        assert(a() = 5)

        let f = [1;2;3;4;5] |> enumerate
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        // f() |> printf "val = %i" // This line will throw



    /// pairwise [1..3] -> seq [(1, 2); (2, 3);]
    /// pairwiseWinding [1..3] -> seq [(1, 2); (2, 3); (3, 1)]
    let pairwiseWinding xs =
        let xs = xs |> List.ofSeq
        match xs with
        | [] -> Seq.empty
        | h::[] -> [h, h] |> Seq.ofList
        | h::ts -> xs @ [h] |> Seq.pairwise

    /// pairwise [1..4] -> seq [(1, 2); (2, 3); (3, 4);]
    /// pairwiseWinding [1..4] -> seq [(1, 2); (2, 3); (3, 4); (4, 1)]
    /// pairwiseWindingFull [1..4] -> seq [(1, 2); (1, 3); (1, 4); (2, 3); (2, 4); (3, 4)]
    /// https://stackoverflow.com/questions/1222185/most-elegant-combinations-of-elements-in-f
    let pairwiseWindingFull xs =
        let xs = xs |> List.ofSeq
        let rec comb n l =
            match n, l with
            | 0, _ -> [[]]
            | _, [] -> []
            | k, (x::xs) -> List.map ((@) [x]) (comb (k-1) xs) @ comb k xs
        comb 2 xs
        |> Seq.map(fun f -> f |> Seq.head, f |> Seq.last)

    // http://www.fssnip.net/50/title/Seqtriplewise
    /// triplewise [1..4] // -> seq [(1, 2, 3); (2, 3, 4)]
    let triplewise (xs: seq<_>) =
        seq {
            use e = xs.GetEnumerator()
            if e.MoveNext() then
                let i = ref e.Current
                if e.MoveNext() then
                    let j = ref e.Current
                    while e.MoveNext() do
                        let k = e.Current
                        yield (!i, !j, k)
                        i := !j
                        j := k }

    let quadwise (xs: seq<_>) =
        seq {
            use e = xs.GetEnumerator()
            if e.MoveNext() then
                let i = ref e.Current
                if e.MoveNext() then
                    let j = ref e.Current
                    if e.MoveNext() then
                        let k = ref e.Current
                        while e.MoveNext() do
                            let l = e.Current
                            yield (!i, !j, !k, l)
                            i := !j
                            j := !k
                            k := l}




    // http://www.fssnip.net/6A/title/SeqgroupWhen-function
    /// Iterates over elements of the input sequence and groups adjacent elements.
    /// A new group is started when the specified predicate holds about the element
    /// of the sequence (and at the beginning of the iteration).
    ///
    /// For example:
    ///  *  Seq.groupWhen isOdd [3;3;2;4;1;2] = seq [[3]; [3; 2; 4]; [1; 2]]
    ///  *  [1..10] |> groupWhen (fun n -> n%3 = 0) // ==> seq [[1; 2]; [3; 4; 5]; [6; 7; 8]; [9; 10]]
    let groupWhen f (xs:seq<_>) =
        seq {
            use en = xs.GetEnumerator()
            let running = ref true

            // Generate a group starting with the current element. Stops generating
            // when it founds element such that 'f en.Current' is 'true'
            let rec group() =  [
                yield en.Current
                if en.MoveNext() then
                    if not (f en.Current) then yield! group()
                else running := false ]

            if en.MoveNext() then
                // While there are still elements, start a new group
                while running.Value do
                    yield group() |> Seq.ofList }


    /// https://stackoverflow.com/questions/2279095/f-split-list-into-sublists-based-on-comparison-of-adjacent-elements
    /// F# list 를 서로 인접한 두 element 에 대해서 주어진 predicate 으로 test 해서 split 한다.
    /// [1; 1; 3;3;3; 2; 3; 4;4;] @ [5..10] |> splitOn (<>);;
    ///    ==>  [[1; 1]; [3; 3; 3]; [2]; [3]; [4; 4]; [5]; [6]; [7]; [8]; [9]; [10]]
    let splitOn predicate sequ =
        let lst = sequ |> List.ofSeq
        List.foldBack (fun el lst ->
                match lst with
                | [] -> [[el]]
                | (x::xs)::ys when not (predicate el x) -> (el::(x::xs))::ys
                | _ -> [el]::lst
             )  lst []




    /// 주어진 sequence 에서 keyselector 로 mapping된 항목이 중복되면 해당 item 에 대해 f 를 수행하고,
    /// 최종적으로는 원래의 sequence 에서 keySelector 에 의한 중복을 제거한 sequence 를 반환한다.
    let onDuplicate (keySelector:'a->'b) f (xs:'a seq) =
        let hash = HashSet()
        seq {
            for item in xs do
                let key = keySelector(item)
                if hash.Contains(key) then
                    f item |> ignore
                else
                    hash.Add(key) |> ignore
                    yield item
        }

    let isAscending  xs = xs |> Seq.pairwise |> Seq.forall (fun (a, b) -> a <= b)
    let isDescending xs = xs |> Seq.pairwise |> Seq.forall (fun (a, b) -> a >= b)

    /// Debug 모드에서만 lazy evaluation 을 eager evaluation 으로 수행
    /// 최대한 lazy evaluation 을 유지하면서 한번만 evaluation 을 원할 경우, Seq.cache 를 이용
    let eagerOnDebug (xs: seq<_>) =
#if DEBUG
        xs |> Array.ofSeq |> seq
#else
        xs
#endif
    /// Seq.min 의 option type safe version : empty sequence 인 경우 None 반환
    let tryMinBy f (xs: seq<_>) =
        if Seq.isEmpty xs then
            None
        else
            xs |> Seq.minBy f |> Some
    /// Seq.reduce 의 option type safe version : empty sequence 인 경우 None 반환
    let tryReduce f (xs: seq<_>) =
        if Seq.isEmpty xs then
            None
        else
            xs |> Seq.reduce f |> Some

    //let ofType<'a>(xs:_ seq) = xs.OfType<'a>()
    let ofType<'a>(xs:IEnumerable) = xs.OfType<'a>()
    let ofNotType<'a>(xs:'b seq) =
        let ofs = xs.OfType<'a>().Cast<'b>()
        xs.Except(ofs)

    /// Sequence 에 뭐라도 있는지 검사
    let any xs = not <| Seq.isEmpty xs
    let nonEmpty = any

    /// http://www.fssnip.net/7Oz/title/Choose-while-function
    /// A function that is like 'Seq.choose' but stops producing values as soon as the first 'None' value is produced.
    let chooseWhile f (xs:seq<_>) =
      seq { use en = xs.GetEnumerator()
            let mutable finished = false
            while not finished do
              if en.MoveNext() then
                match f en.Current with
                | None -> finished <- true
                | Some v -> yield v
              else
                finished <- true }

    let choosei (chooser:int -> 't -> 'u option) (xs:seq<'t>) =
        xs |> Seq.mapi chooser |> Seq.choose id

    let picki (chooser:int -> 't -> 'u option) (xs:seq<'t>) =
        xs |> Seq.mapi chooser |> Seq.pick id


    /// index 와 내용으로 fitering
    let filteri (f:int -> 't -> bool) (xs:seq<'t>) =
        xs
        |> Seq.indexed
        |> Seq.filter (fun tpl -> tpl ||> f)
        |> Seq.map snd

    /// Array.partition 과 동일한 기능을 Seq.partition 으로 shortcut
    let partition pred xs =
        xs
        |> Array.ofSeq
        |> Array.partition pred

    /// 다중 seq 전체의 intersection 구하기.  https://stackoverflow.com/questions/1674742/intersection-of-multiple-lists-with-ienumerable-intersect 참고
    // F# 의 Set 기반 intersectMany 사용시, 객체가 IComparable 과 같은 interface 를 구현해야 하므로 번거로움.
    // HashSet 기반으로 구현 : NOT thread safe!!
    let intersectMany (xss:seq<#seq<'a>>) =
        let xss = xss |> List.ofSeq
        match xss with
        | [] -> []
        | h::t ->
            let hash = new HashSet<'a>(h |> List.ofSeq)
            for s in t do
                hash.IntersectWith(s)
            hash |> List.ofSeq

    /// LINQ 기반의 difference
    /// xs1 - xs2
    let difference xs1 xs2 = Enumerable.Except(xs1, xs2) |> seq // xs1 - xs2
    /// Set diff : 방향이 pipe 에 맞게 되어 있음.
    /// xs2 - xs1.  [1..10] |> setDifferencePipe [3..10] --> [1;2]
    let differencePipe xs1 xs2 = difference xs2 xs1
    /// LINQ 기반의 intersect
    // https://stackoverflow.com/questions/13561301/intersection-between-two-lists-f
    let intersect xs1 xs2 = Enumerable.Intersect(xs1, xs2) |> seq
    let symetricDifference xs1 xs2 = Enumerable.Union(Enumerable.Except(xs1, xs2), Enumerable.Except(xs2, xs1)) |> seq

    let setEqual xs1 xs2 = symetricDifference xs1 xs2 |> Seq.isEmpty



    /// xs 중에서 predicate 을 만족하는 요소의 index 를 반환
    let findIndices predicate xs =
        xs
        |> Seq.indexed
        |> Seq.choose (fun (i, x) -> if predicate x then Some i else None)

    /// haystack 에 needles 이 모두 포함되어 있는지
    let containsAllOf (haystack:'a seq) (needles:'a seq) =
        let hash = needles |> HashSet
        hash.IsProperSubsetOf(haystack)
    let containsAllOfPipe haystack = flipf containsAllOf haystack
    let containsAnyOf (haystack:'a seq) (needles:'a seq) =
        let hash = needles |> HashSet
        haystack |> Seq.exists (fun h -> hash.Contains(h))
    let containsAnyOfPipe haystack = flipf containsAnyOf haystack


[<AutoOpen>]
module SeqModule =
    let counterGenerator = Seq.counterGenerator
    let incrementalKeywordGenerator = Seq.incrementalKeywordGenerator


module private testMe =
    let ofType<'a>(xs:_ seq) = xs.OfType<'a>()
    let ofNotType<'a>(xs:'b seq) =
        let ofs = xs.OfType<'a>().Cast<'b>()
        xs.Except(ofs)

    let xs = [1:>obj; 'a'; "hello"; DateTime.Now]
    let strings     = xs.OfType<string>()
    let strings2    = xs |> ofType<string> |> Array.ofSeq
    let nonStrings  = xs |> ofNotType<string> |> Array.ofSeq
    let strings3    = ofType<string> xs |> Array.ofSeq
    let nonStrings2 = ofNotType<string> xs |> Array.ofSeq
    ()