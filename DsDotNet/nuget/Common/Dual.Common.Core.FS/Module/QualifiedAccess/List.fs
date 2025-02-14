namespace Dual.Common.Core.FS

open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module ListPrelude =
    /// List.empty
    let lempty = List.empty

[<RequireQualifiedAccess>]
module List =
    /// List 중에서 type 에 맞는 것만 골라냄
    let ofType<'a> source : 'a list =
        Seq.ofType<'a> source |> List.ofSeq

    /// Seq 중에서 주어진 type 이 아닌 것만 골라냄.
    /// obj 의 sequence 로 반환되므로 필요시 다시 casting 해서 써야 한다.
    let ofNotType<'a> source =
        Seq.ofNotType<'a> source |> List.ofSeq

    /// Seq 중에서 type 에 맞는 것만 골라냄.  ofType<'a> 과 동일
    let whereType<'a> = ofType<'a>
    /// Seq 중에서 주어진 type 이 아닌 것만 골라냄.   ofNotType<'a> 과 동일
    let whereNotType<'a> = ofNotType<'a>

    /// List.collect 와 동일
    let bind = List.collect

    let choosei (chooser:int -> 't -> 'u option) (xs: 't list) =
        xs |> Seq.choosei chooser |> List.ofSeq

    let picki (chooser:int -> 't -> 'u option) (xs: 't list) =
        xs |> Seq.picki chooser |> List.ofSeq

    /// index 와 내용으로 fitering
    let filteri (f:int -> 't -> bool) (xs: 't list) =
        xs |> Seq.filteri f |> List.ofSeq


    let cast<'a> source =
        source |> Seq.cast<'a> |> List.ofSeq

    /// list 목록 중에 마지막을 제외한 것
    let initv source =
        match source with
        | [] -> []
        | _ -> source |> List.rev |> List.tail |> List.rev

    let takeUntil f xs = List.takeWhile (f >> not) xs
    /// List.collect id 적용 : [[xs]] -> [xs]
    let flatten xss = xss |> List.collect id

    /// 주어진 list 에서 조건을 만족하는 갯수를 반환
    let count (predicate:'a -> bool) (xs:'a list) = xs |> List.filter predicate |> List.length

    /// List 에 뭐라도 있는지 검사
    let any xs = not <| List.isEmpty xs

    /// 두 list 가 같은지 비교
    let equal = Seq.equal

    /// 다중 seq 전체의 intersection 구하기.
    let intersectMany = Seq.intersectMany

    let difference         xs1 xs2 = Seq.difference         xs1 xs2 |> List.ofSeq
    let differencePipe     xs1 xs2 = Seq.differencePipe     xs1 xs2 |> List.ofSeq
    let intersect          xs1 xs2 = Seq.intersect          xs1 xs2 |> List.ofSeq
    let symetricDifference xs1 xs2 = Seq.symetricDifference xs1 xs2 |> List.ofSeq

    /// list 목록 중에 처음과 마지막을 제외한 것
    let mid source =
        match source with
        | [] -> []
        | _ -> source |> List.tail |> initv

    /// [1..10] |> startsWith [1..3] ==> true
    /// [1..3] |> startsWith [1..10] ==> false
    let startsWith (xs1:'a list) (xs2:'a list) =
        xs1.Length <= xs2.Length && Seq.forall2 (=) xs1 xs2     // 반드시 List.forall2 가 아니라, Seq.forall2 를 사용해야 함.  List.forall2 의 경우, 길이가 다르면 exceptin 발생.  Seq.forwall 은 길이 무시

    /// List.foldBack 과 동일하나, list(=xs) 의 pipe line 이 가능하도록 list 를 맨 마지막 인자로 수정
    let FoldBack folder state xs = List.foldBack folder xs state
    /// List.scanBack 과 동일하나, list(=xs) 의 pipe line 이 가능하도록 list 를 맨 마지막 인자로 수정
    let ScanBack folder state xs = List.scanBack folder xs state
    /// Option list 에서 Some 인 항목에 대해서만 mapper 적용한 list 반환
    let mapSome mapper (options:'a option list) = options |> List.choose (Option.map mapper)

    let mapTuple (mapper1:'a->'c) (mapper2:'b->'d) (xs:('a*'b) list) =
        xs |> List.map (fun (a, b) -> mapper1 a, mapper2 b)

    /// tuple 의 list 에 대해서 tuple 의 fst 만 mapping
    let map1st mapper = mapTuple mapper id

    // [ ("a", 1); ("b", 2) ] |> map2nd ((+) 1) ==> [("a", 2); ("b", 3)]
    /// tuple 의 list 에 대해서 tuple 의 snd 만 mapping
    let map2nd mapper = mapTuple id mapper

    /// tuple 의 fst projection 후, mapper 적용
    let mapProject1st mapper (xs:('a*_) list) = xs |> List.map (fst >> mapper)
    /// tuple 의 snd projection 후, mapper 적용
    let mapProject2nd mapper (xs:(_*'a) list) = xs |> List.map (snd >> mapper)

    /// list 의 list 에 대해서 내부의 list 를 mapping
    ///
    /// [[1..3]; [5..9]] |> List.mapInner ((+) 1) ==> [[2; 3; 4]; [6; 7; 8; 9; 10]]
    let mapInner (mapper:'x->'b) (xss:'x list list) =
        xss |> List.map (List.map mapper)

    /// list 의 list 에 대해서 내부의 list 에 List.mapi 적용
    let mapiInner (mapper:int -> 'x->'b) (xss:'x list list) =
        xss |> List.map (List.mapi mapper)

    /// list 의 list 에 대해서 내부의 list 에 List.mapi 적용
    let collectiInner (mapper:int -> 'x->'b) (xss:'x list list) =
        xss |> List.map (List.mapi mapper)

    /// xs 중에서 predicate 을 만족하는 요소의 index 를 반환
    let findIndices predicate xs = Seq.findIndices predicate xs |> List.ofSeq

    /// list xs 의 맨 뒤에 x 를 add
    let addLast x xs = x::(xs |> List.rev) |> List.rev

    /// List 의 모든 항목에 대해 f 를 적용했을 때, 모두 Some 값이면, 적용한 결과 반환
    /// 하나라도 f 적용에서 None 반환하면 None
    // http://www.fssnip.net/s4/title/Reinventing-the-Reader-Monad
    // Reinventing the Reader Monad
    // Alternative solution to the problem in Scott Wlaschin's "Reinventing the Reader Monad" article (but without monads).
    // Applies the specified function to all items in the list.
    // If any of the function calls results in a Failure, the
    // map function returns failure immediately.
    let mapOrFail f lst =
        let rec loop acc items =
            match items with
            | x::xs ->
                match f x with
                | Some r -> loop (r::acc) xs
                | None -> None
            | [] -> Some(List.rev acc)
        loop [] lst

    /// List.reduce 의 option type safe version : empty sequence 인 경우 None 반환
    let tryReduce f (xs: list<_>) =
        if List.isEmpty xs then
            None
        else
            xs |> List.reduce f |> Some
    let tryReduceBack f (xs: list<_>) =
        if List.isEmpty xs then
            None
        else
            xs |> List.reduceBack f |> Some

    let orElse ys xs = if List.isEmpty xs then ys else xs
    let orElseWith f xs = if List.isEmpty xs then f() else xs


    /// System.Reactive 의 Observable.Window 를 참고해서 count 와 skip 을 구현
    /// Sliding / Hopping window 지원
    /// List.windowed 함수의 경우, skip 이 1 로 고정된 형태이고, count 갯수가 동일한 chunk 까지만 모으는데 반해,
    /// windowed2 는 count 와 skip 을 받고, 맨 마지막 chunk 는 count 보다 작은 갯수를 허용한다.
    /// Rx.NET in action.pdf, pp227
    let rec windowed2 count skip (xs:'x list) =
        [
            if xs.Length >= count then
                yield xs |> List.take count
                yield! xs |> List.skip skip |> windowed2 count skip
            else
                yield xs
        ]

    /// separator 항목 기준으로 split.
    /// e.g:
    ///
    /// 입력: [ 1; 0; 2; 3; 0; 10; 0 ] |> List.splitWith (fun x -> x = 0)
    ///
    /// 출력: [   [1]; [2; 3]; [10]  ]
    let splitWith (f: 'x -> bool) (xs: 'x list) : 'x list list =
        ( ([], []), xs )
        ||> List.fold (fun (acc, current) v ->
            if f v then
                match current with
                | [] -> (acc, [])  // 현재 리스트가 비어 있으면 그대로 유지
                | _ -> (acc @ [current], [])  // 현재 리스트가 비어있지 않으면 acc에 추가하고 새로운 구간 시작
            else
                (acc, current @ [v])  // 현재 구간에 값을 추가
        ) |> fun (acc, current) ->
            match current with
            | [] -> acc  // 마지막 구간이 비어있으면 그대로 반환
            | _ -> acc @ [current]  // 마지막 구간이 있으면 acc에 추가

    /// separator 항목 기준으로 split.
    /// e.g:
    ///
    /// 입력: [ 1; 0; 2; 3; 0; 10; 0 ] |> List.splitBy 0
    ///
    /// 출력: [   [1]; [2; 3]; [10]  ]
    let splitBy (x: 'a) (xs: 'a list) : 'a list list = splitWith ((=) x) xs


    module private TestMe =
        let isEven x = if x % 2 = 0 then Some x else None
        let a = [2; 4; 8] |> mapOrFail isEven
        assert( a = Some[2; 4; 8])
        let b = [2; 4; 8; 3; 4] |> mapOrFail isEven
        assert( b = None)

        let evens = [1..10] |> findIndices (fun x -> x % 2 = 0)
        assert(evens = [1; 3; 5; 7; 9])
