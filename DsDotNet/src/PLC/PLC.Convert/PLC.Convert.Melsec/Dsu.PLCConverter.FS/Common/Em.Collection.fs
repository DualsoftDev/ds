namespace Dsu.PLCConverter.FS

open System.Linq
open System.Collections.Generic


[<AutoOpen>]
module EnumuerableExt =
    // instead Seq<'T>
    type IEnumerable<'T> with
        member x.isEmpty() = Seq.isEmpty x
        member x.length() = Seq.length x
        member x.map f = Seq.map f x
        member x.any() = Seq.isEmpty x |> not 
        member x.any f = Seq.tryFind f x |> Option.isSome

        member x.select f = Seq.map f x
        member x.selectMany f = Seq.collect f x
        member x.where f = Seq.filter f x
        member x.realize() = Array.ofSeq x |> ignore
        member x.nonNullAny() = x <> null && x.Any()
        member x.isNullOrEmpty() = x = null || Seq.isEmpty x

    /// append two sequence
    let (@@) = Seq.append
    //type array<'a> with
    //    member x.IsEmpty = Array.isEmpty

[<AutoOpen>]
module ArrayExt =
    /// append two array
    let (@@@) = Array.append


[<AutoOpen>]
module ResizeArrayExt =
    module ResizeArray =
        let ofSeq (x:_ seq) = ResizeArray(x)

[<AutoOpen>]
module Map =
    let keys x = x |> Map.toSeq |> Seq.map fst
    let values x = x |> Map.toSeq |> Seq.map snd

    type Map<'k, 'v> when 'k: comparison with
        member x.keys with get() = x |> Map.toSeq |> Seq.map fst
        member x.values with get() = x |> Map.toSeq |> Seq.map snd
        member x.contains k = x |> Map.exists k
        member x.exists k = x |> Map.exists k

module ChainedMap =
    /// Chained dictionary (다중 dictionary) 에서 항목 찾기
    let findAll maps key =
        maps
        |> Seq.map (Map.tryFind key)
        |> Seq.choose id

    let tryFind maps key =
        maps
        |> Seq.tryPick (Map.tryFind key)

    let find maps key =
        tryFind maps key
        |> Option.get


    #if INTERACTIVE
    let private doTest() =
        let boxSnd(k, v) = k, box(v)
        let dic1 = [ (3, 3); (1, 1); (2, 2); ]               |> Seq.map boxSnd |> Map.ofSeq
        let dic2 = [ (3, "Three"); (4, "Four"); (5, "Five")] |> Seq.map boxSnd |> Map.ofSeq

        findAll [dic1; dic2] 3
        findAll [dic1; dic2] 3

        tryFind [dic1; dic2] 6
        find [dic1; dic2] 6
    #endif

    // 다중 중복 키 허용 dictionary 는 MultiValueDictionary 를 이용
    // Microsoft.Collections.Extensions.MultiValueDictionary

[<RequireQualifiedAccess>]
module Array =
    /// Array type extensions
    // https://stackoverflow.com/questions/11836167/how-to-define-a-type-extension-for-t-in-f
    type 'a ``[]`` with
        member x.GetOrDefault n = 
            if x.Length > n then x.[n]
            else Unchecked.defaultof<'a>

        member x.IsEmpty = Array.isEmpty x

        member x.safeMinBy f =
            if x.IsEmpty then
                None
            else
                x |> Array.minBy f |> Some


    let safeMinBy f (arr: array<'t>) = 
        if Array.isEmpty arr then
            None
        else
            arr |> Array.minBy f |> Some
    let safeMaxBy f (arr: array<'t>) = 
        if Array.isEmpty arr then
            None
        else
            arr |> Array.maxBy f |> Some

    let safeReduce f (arr: array<'t>) = 
        if Array.isEmpty arr then
            None
        else
            arr |> Array.reduce f |> Some
    let any (arr: array<'t>) = not (Array.isEmpty arr) 
    /// Array 중에서 type 에 맞는 것만 골라냄
    let ofType<'a> source : 'a array =
        Seq.ofType<'a> source |> Array.ofSeq

    /// Array 중에서 type 이 아닌 것만 골라냄
    let ofNotType<'a when 'a: equality> source = 
        Seq.ofNotType<'a> source |> Array.ofSeq

    /// Array.collect 와 동일
    let bind = Array.collect

    /// index 와 내용으로 fitering
    let filteri (f:int*'t->bool) (arr: array<'t>) =
        arr |> Seq.filteri f |> Array.ofSeq

    /// 원본 array arr 의 pos 위치에 값 value 를 삽입하여 생성한 사본 array 반환
    let insertAt (arr: array<'t>) (pos:int) (value:'t) =
        let sub1 = Array.sub arr 0 pos
        let sub2 = Array.sub arr pos (arr.Length - pos)
        sub1 @@@ [|value|] @@@ sub2

[<RequireQualifiedAccess>]
module List =
    /// List 중에서 type 에 맞는 것만 골라냄
    let ofType<'a> source : 'a list =
        Seq.ofType<'a> source |> List.ofSeq

    /// List.collect 와 동일
    let bind = List.collect

    /// index 와 내용으로 fitering
    let filteri (f:int*'t->bool) (lst: array<'t>) =
        lst |> Seq.filteri f |> List.ofSeq

    let cast<'a> source =
        source |> Seq.cast<'a> |> List.ofSeq
