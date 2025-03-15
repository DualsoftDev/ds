namespace Dual.Common.FS.LSIS

open System.Linq
open System.Collections.Generic


[<RequireQualifiedAccess>]
module Seq =
    /// 두 sequence 가 같은지 비교
    let equal seq1 seq2 =
        Enumerable.SequenceEqual(seq1, seq2)

    /// pairwise [1..3] -> seq [(1, 2); (2, 3);]
    /// pairwiseWinding [1..3] -> seq [(1, 2); (2, 3); (3, 1)]
    let pairwiseWinding (source: seq<_>) =
        Seq.append source (source |> Seq.take 1) |> Seq.pairwise
    // http://www.fssnip.net/50/title/Seqtriplewise
    /// triplewise [1..4] // -> seq [(1, 2, 3); (2, 3, 4)]
    let triplewise (source: seq<_>) =
        seq {
            use e = source.GetEnumerator() 
            if e.MoveNext() then
                let i = ref e.Current
                if e.MoveNext() then
                    let j = ref e.Current
                    while e.MoveNext() do
                        let k = e.Current 
                        yield (!i, !j, k)
                        i := !j
                        j := k }

    let quadwise (source: seq<_>) =
        seq {
            use e = source.GetEnumerator() 
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


    /// http://www.fssnip.net/7Rr/title/Enumerating-function
    /// Creates a function from a sequence that, when called, returns the items in the sequence
    let enumerate (xs: seq<_>)  =
        use en = xs.GetEnumerator()
        fun () ->
            en.MoveNext() |> ignore
            en.Current

    // http://www.fssnip.net/18/title/Haskell-function-iterate
    /// seed 에 f 를 0번, 1번, ... 무한번 적용한 sequence 반환
    /// Seq.take 10 (iterate ((*)2) 1)
    /// =>  seq [1; 2; 4; 8; ...]
    let rec iterate f seed = seq {
      yield seed
      yield! iterate f (f seed) }


    #if INTERACTIVE
    let test() =
        let f = [1;2;3;4;5] |> enumerate

        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i"
        f() |> printf "val = %i" // This line will throw
    #endif

    // http://www.fssnip.net/7WT/title/SeqtrySkip
    /// Tries to skip the given number of items and returns the rest. Disposes the enumerator at the end.
    /// skip 할 갯수가 seq 의 갯수보다 더 크면 Seq.skip 은 exception 발생하지만, trySkip 은 exception safe
    let trySkip (n : int) (s : _ seq) =
        seq {
            use e = s.GetEnumerator ()
            let mutable i = 0
            let mutable dataAvailable = e.MoveNext ()
            while dataAvailable && i < n do
                dataAvailable <- e.MoveNext ()
                i <- i + 1
            if dataAvailable then
                yield e.Current
                while e.MoveNext () do
                    yield e.Current
        }


    // http://www.fssnip.net/6A/title/SeqgroupWhen-function
    /// Iterates over elements of the input sequence and groups adjacent elements.
    /// A new group is started when the specified predicate holds about the element
    /// of the sequence (and at the beginning of the iteration).
    ///
    /// For example: 
    ///  *  Seq.groupWhen isOdd [3;3;2;4;1;2] = seq [[3]; [3; 2; 4]; [1; 2]]
    ///  *  [1..10] |> groupWhen (fun n -> n%3 = 0) // ==> seq [[1; 2]; [3; 4; 5]; [6; 7; 8]; [9; 10]]
    let groupWhen f (input:seq<_>) =
        seq {
            use en = input.GetEnumerator()
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
    let onDuplicate (keySelector:'a->'b) f (sequ:'a seq) =
        let hash = HashSet()
        seq {
            for item in sequ do
                let key = keySelector(item)
                if hash.Contains(key) then
                    f item |> ignore
                else
                    hash.Add(key) |> ignore
                    yield item
        }

    let isAscending sequ  = sequ |> Seq.pairwise |> Seq.forall (fun (a, b) -> a <= b) 
    let isDescending sequ = sequ |> Seq.pairwise |> Seq.forall (fun (a, b) -> a >= b) 



    /// Debug 모드에서만 lazy evaluation 을 eager evaluation 으로 수행
    let eagerOnDebug (sequ: seq<'t>) = 
#if DEBUG
        sequ |> Array.ofSeq |> seq
#else
        sequ
#endif
    /// Seq.min 의 option type safe version : empty sequence 인 경우 None 반환
    let safeMinBy f (sequ: seq<'t>) = 
        if Seq.isEmpty sequ then
            None
        else
            sequ |> Seq.minBy f |> Some
    /// Seq.reduce 의 option type safe version : empty sequence 인 경우 None 반환
    let safeReduce f (sequ: seq<'t>) = 
        if Seq.isEmpty sequ then
            None
        else
            sequ |> Seq.reduce f |> Some

    // https://stackoverflow.com/questions/2521254/f-equivalent-to-enumerable-oftypea
    let ofType111<'a> (items: _ seq) = items |> Seq.cast<obj> |> Seq.filter(fun x -> x :? 'a) |> Seq.cast<'a>

    // https://gist.github.com/kos59125/3780229
    /// Seq 중에서 type 에 맞는 것만 골라냄
    let ofType<'a> (source : System.Collections.IEnumerable) : seq<'a> =
       let resultType = typeof<'a>
       seq {
          for item in source do
             match item with
                | null -> ()
                | _ ->
                   if resultType.IsAssignableFrom (item.GetType ()) then
                      yield (downcast item)
    }
    /// Seq 중에서 주어진 type 이 아닌 것만 골라냄.
    /// obj 의 sequence 로 반환되므로 필요시 다시 casting 해서 써야 한다.
    let ofNotType<'a> (source : System.Collections.IEnumerable) =
       let resultType = typeof<'a>
       seq {
          for item in source do
             match item with
                | null -> ()
                | _ ->
                   if not <| resultType.IsAssignableFrom (item.GetType ()) then
                      yield item
    }

    /// http://www.fssnip.net/7Oz/title/Choose-while-function
    /// A function that is like 'Seq.choose' but stops producing values as soon as the first 'None' value is produced.
    let chooseWhile f (input:seq<_>) =
      seq { use en = input.GetEnumerator()
            let mutable finished = false
            while not finished do
              if en.MoveNext() then
                match f en.Current with
                | None -> finished <- true
                | Some v -> yield v
              else
                finished <- true }


    /// Seq.collect 와 동일
    let bind = Seq.collect

    /// Sequence 에 뭐라도 있는지 검사
    let any sequ = not (sequ |> Seq.isEmpty)

    /// index 와 내용으로 fitering
    let filteri (f:int*'t->bool) (sequ: seq<'t>) =
        sequ 
        |> Seq.indexed
        |> Seq.filter f
        |> Seq.map snd

    /// Array.partition 과 동일한 기능을 Seq.partition 으로 shortcut
    let partition pred sequ =
        sequ
        |> Array.ofSeq
        |> Array.partition pred

    /// nesting 된 inner seq 에 대해서 function f 를 map
    /// [[1;2]; [2;3]] |> innerSeqMap (Seq.map (( *) 2))  --> {{2; 4}; {4; 6}}
    /// [[1;2]; [2;3]] |> innerSeqMap Seq.sum  --> {3; 5}
    let innerSeqMap f (seqseq:seq<#seq<'a>>) =
        seqseq
        |> Seq.map f

    /// netsting 된 inner seq 의 각 element 에 대해서 function f 를 map
    // [[1;2]; [2;3]] |> innerElementMap (( *) 2)       --> {{2;4}; {4; 6}}
    let innerElementMap f seqseq =
        innerSeqMap (Seq.map f) seqseq

    /// nesting 된 inner seq 를 array 로 변환
    /// [[1;2]; [2;3]] |> innerSeq2Array  --> { [|1; 2|]; [|2; 3|] }
    let innerSeq2Array seqseq = innerSeqMap Array.ofSeq seqseq
