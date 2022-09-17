namespace Engine.Common.FS

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module ArrayPrelude =
    /// append two array
    let (@@@) = Array.append
    /// Array.empty
    let aempty = Array.empty

[<Extension>] // type ArrayExt =
type ArrayExt =
    [<Extension>] static member GetReversed(xs:'a []) = xs |> Array.rev

[<RequireQualifiedAccess>]
module Array =
    /// Array type extensions
    // https://stackoverflow.com/questions/11836167/how-to-define-a-type-extension-for-t-in-f
    type 'a ``[]`` with
        member x.GetOrDefault n = 
            if x.Length > n then x.[n]
            else Unchecked.defaultof<'a>

        member x.IsEmpty = Array.isEmpty x

        member x.tryMinBy f =
            if x.IsEmpty then
                None
            else
                x |> Array.minBy f |> Some


    let tryMinBy f (arr: array<'t>) = 
        if Array.isEmpty arr then
            None
        else
            arr |> Array.minBy f |> Some
    let tryMaxBy f (arr: array<'t>) = 
        if Array.isEmpty arr then
            None
        else
            arr |> Array.maxBy f |> Some

    let tryReduce f (arr: array<'t>) = 
        if Array.isEmpty arr then
            None
        else
            arr |> Array.reduce f |> Some
    let any (arr: array<'t>) = not (Array.isEmpty arr) 
    /// Array 중에서 type 에 맞는 것만 골라냄
    let ofType<'a> source : 'a array =
        Seq.ofType<'a> source |> Array.ofSeq

    /// Array 중에서 type 이 아닌 것만 골라냄
    /// obj 의 sequence 로 반환되므로 필요시 다시 casting 해서 써야 한다.
    let ofNotType<'a when 'a: equality> source = 
        Seq.ofNotType<'a> source |> Array.ofSeq

    /// Seq 중에서 type 에 맞는 것만 골라냄.  ofType<'a> 과 동일
    let whereType<'a> = ofType<'a>
    /// Seq 중에서 주어진 type 이 아닌 것만 골라냄.   ofNotType<'a> 과 동일
    let whereNotType<'a when 'a: equality> = ofNotType<'a>

    /// Array.collect 와 동일
    let bind = Array.collect

    /// Array.collect id 적용 : {{xs}} -> {xs}
    let flatten xss = xss |> Array.collect id

    let choosei (chooser:int -> 't -> 'u option) (xs: array<'t>) =
        xs |> Seq.choosei chooser |> Array.ofSeq

    let picki (chooser:int -> 't -> 'u option) (xs: array<'t>) =
        xs |> Seq.picki chooser |> Array.ofSeq

    /// index 와 내용으로 fitering
    let filteri (f:int -> 't -> bool) (xs: array<'t>) =
        xs |> Seq.filteri f |> Array.ofSeq

    let append<'a> (xs:'a array) (x:'a) = xs.Append(x).ToArray()

    /// 원본 array arr 의 pos 위치에 값 value 를 삽입하여 생성한 사본 array 반환
    let insertAt (arr: array<'t>) (pos:int) (value:'t) =
        let sub1 = Array.sub arr 0 pos
        let sub2 = Array.sub arr pos (arr.Length - pos)
        sub1 @@@ [|value|] @@@ sub2


    let mapTuple (mapper1:'a->'c) (mapper2:'b->'d) (xs:('a*'b) array) =
        xs |> Array.map (fun (a, b) -> mapper1 a, mapper2 b)

    /// tuple array 에서 tuple 의 첫 항목들만 mapping 수행
    ///
    /// [ (1, "a"); (2, "b") ] |> map1st ((+) 1) ==> [(2, "a"); (3, "b")]
    let map1st mapper = mapTuple mapper id

    /// tuple 의 array 에 대해서 tuple 의 snd 만 mapping 
    ///
    // [ ("a", 1); ("b", 2) ] |> map2nd ((+) 1) ==> [("a", 2); ("b", 3)]
    let map2nd mapper = mapTuple id mapper
        /// tuple 의 fst projection 후, mapper 적용
    let mapProject1st mapper (xs:('a*_) array) = xs |> Array.map (fst >> mapper)
    /// tuple 의 snd projection 후, mapper 적용
    let mapProject2nd mapper (xs:(_*'a) array) = xs |> Array.map (snd >> mapper)

    /// System.Reactive 의 Observable.Window 를 참고해서 count 와 skip 을 구현
    /// Sliding / Hopping window 지원
    /// List.windowed 함수의 경우, skip 이 1 로 고정된 형태이고, count 갯수가 동일한 chunk 까지만 모으는데 반해,
    /// windowed2 는 count 와 skip 을 받고, 맨 마지막 chunk 는 count 보다 작은 갯수를 허용한다.
    /// Rx.NET in action.pdf, pp227
    let rec windowed2 count skip (xs:'x array) =
        [
            if xs.Length >= count then
                yield xs |> Array.take count
                yield! xs |> Array.skip skip |> windowed2 count skip
            else
                yield xs
        ]
