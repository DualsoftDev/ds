namespace Dual.Common.Core.FS

open System.Collections.Generic
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module EmFSharpCollectionModule =
    open Microsoft.FSharp.Collections

    (* F# list extension *)
    type List<'T> with
        /// 'T list 을 Option<'T list> 로 변환.  [] -> None 반환
        member xs.SeqToOption() = if xs.IsNullOrEmpty() then None else Some xs
        /// xs 가 empty 이면 ys 반환.  emtpy 아니면 xs 그대로..
        member xs.OrElse(ys): List<'T> = xs.SeqToOption() |? ys
        /// xs 가 empty 이면 onEmpty 적용 결과 반환.  emtpy 아니면 xs 그대로..
        member xs.OrElseWith(onEmpty: unit -> 'T list): List<'T> = xs.SeqToOption().DefaultWith(onEmpty)
        member xs.GetReversed() = xs |> List.rev

    (* Array<'T> extension *)
    // https://stackoverflow.com/questions/18359825/f-how-to-extended-the-generic-array-type
    // https://stackoverflow.com/questions/11836167/how-to-define-a-type-extension-for-t-in-f
    type 'T ``[]`` with
        /// 'T [] 을 Option<'T []> 로 변환.  [] -> None 반환
        member xs.SeqToOption() = if xs.IsNullOrEmpty() then None else Some xs
        /// xs 가 empty 이면 ys 반환.  emtpy 아니면 xs 그대로..
        member xs.OrElse(ys): 'T[] = xs.SeqToOption() |? ys
        /// xs 가 empty 이면 onEmpty 적용 결과 반환.  emtpy 아니면 xs 그대로..
        member xs.OrElseWith(onEmpty: unit -> 'T []): 'T[] = xs.SeqToOption().DefaultWith(onEmpty)
        member xs.GetReversed() = xs |> Array.rev

    type Option<'T> with
        member xs.OrElse(option)     = Option.orElse option xs
        member xs.OrElseWith(ifNone) = Option.orElseWith ifNone xs
        member xs.IfNone(f)          = if xs |> Option.isNone then f()


    type FSharpListExt =
        // 일반 F# type extension 구현시, 다음 오류 발생해서 [<Extension>] 으로 사용
        // error FS0340: 형식 매개 변수 'T'의 선언을 사용하려면 'T: comparison 형식의 제약 조건이 필요하므로 시그니처와 구현이 호환되지 않습니다.
        [<Extension>] static member Sort            (xs:List<'T>) = xs |> List.sort
        [<Extension>] static member SortDescending  (xs:List<'T>) = xs |> List.sortDescending


    type List<'T> with
        member xs.SortBy          (projection) = xs |> List.sortBy projection
        member xs.SortByDescending(projection) = xs |> List.sortByDescending projection
        member xs.SortWith        (comparer)   = xs |> List.sortWith comparer
        member xs.SplitAt         (index)      = xs |> List.splitAt index
        member xs.SplitInto       (count)      = xs |> List.splitInto count

    (* Seq<'T> extension *)
    (* Net9.0 부터 지원되지 않는 형식
    type IEnumerable<'T> with
        /// collection<'T> 을 Option<collection<'T>> 로 변환.  [] -> None 반환
        member xs.ToOption() = if xs.IsNullOrEmpty() then None else Some xs
        /// xs 가 empty 이면 ys 반환.  emtpy 아니면 xs 그대로..
        member xs.OrElse(ys) = xs.ToOption() |? ys
        /// xs 가 empty 이면 onEmpty 적용 결과 반환.  emtpy 아니면 xs 그대로..
        member xs.OrElseWith(onEmpty: unit -> 'T seq) = xs.ToOption().DefaultWith(onEmpty)
        /// xs[n] 항목 반환.  불가능하면 defautl 값 반환.
        member xs.GetOrDefault n = xs.TryItem n |? Unchecked.defaultof<'T>
        member xs.IsEmpty = Seq.isEmpty xs

        member xs.GetReversed() = xs |> Seq.rev


        /// Seq.min 의 option type safe version : empty sequence 인 경우 None 반환
        member xs.TryMinBy f      = xs.ToOption() |> Option.map (Seq.minBy f)
        /// Seq.reduce 의 option type safe version : empty sequence 인 경우 None 반환
        member xs.TryReduce f     = xs.ToOption() |> Option.map (Seq.reduce f)
        member xs.TryReduceBack f = xs.ToOption() |> Option.map (Seq.reduceBack f)


        /// xs 가 비어있으면 f() 실행
        member xs.IfEmpty(f: unit -> unit) = if Seq.isEmpty xs then f()
        /// xs 의 맨 첫 항목 반환, 없으면 y 반환
        member xs.HeadOr(y) = xs.TryHead().DefaultValue(y)
        /// xs 의 맨 마지막 항목 반환, 없으면 y 반환
        member xs.LastOr(y) = xs.TryLast().DefaultValue(y)
    *)

type Net90Extension =
    /// collection<'T> 을 Option<collection<'T>> 로 변환.  [] -> None 반환.
    ///
    /// 일반적인 ToOption() 과는 의미가 달라서 SeqToOption() 으로 명명
    [<Extension>] static member SeqToOption(xs:IEnumerable<'T>) = if xs.IsNullOrEmpty() then None else Some xs

    /// xs 가 empty 이면 ys 반환.  emtpy 아니면 xs 그대로..
    [<Extension>] static member OrElse(xs:IEnumerable<'T>, ys:IEnumerable<'T>) = if xs.IsNullOrEmpty() then ys else xs

    /// xs 가 empty 이면 onEmpty 적용 결과 반환.  emtpy 아니면 xs 그대로..
    [<Extension>] static member OrElseWith(xs:IEnumerable<'T>, onEmpty: unit -> 'T seq) = if xs.IsNullOrEmpty() then onEmpty() else xs

    /// xs[n] 항목 반환.  불가능하면 defautl 값 반환.
    [<Extension>] static member GetOrDefault(xs:IEnumerable<'T>, n:int) = xs.TryItem n |? Unchecked.defaultof<'T>

    [<Extension>] static member GetReversed(xs:IEnumerable<'T>, n:int) = xs |> Seq.rev


    /// Seq.min 의 option type safe version : empty sequence 인 경우 None 반환
    [<Extension>] static member TryMinBy(xs:IEnumerable<'T>, f) = if xs.IsNullOrEmpty() then None else Some (xs |> Seq.minBy f)

    /// Seq.reduce 의 option type safe version : empty sequence 인 경우 None 반환
    [<Extension>] static member TryReduce(xs:IEnumerable<'T>, f) = if xs.IsNullOrEmpty() then None else Some (xs |> Seq.reduce f)

    [<Extension>] static member TryReduceBack(xs:IEnumerable<'T>, f) = if xs.IsNullOrEmpty() then None else Some (xs |> Seq.reduceBack f)


    /// xs 가 비어있으면 f() 실행
    [<Extension>] static member IfEmpty(xs:IEnumerable<'T>, f: unit -> unit) = if Seq.isEmpty xs then f()

    /// xs 의 맨 첫 항목 반환, 없으면 y 반환
    [<Extension>] static member HeadOr(xs:IEnumerable<'T>, y) = xs.TryHead() |? y

    /// xs 의 맨 마지막 항목 반환, 없으면 y 반환
    [<Extension>] static member LastOr(xs:IEnumerable<'T>, y) = xs.TryLast() |? y


    //// { 임시 : 삭제 대상

    //[<Obsolete("Net9.0 부터 지원되지 않는 형식: IsEmpty() 사용")>]
    //[<Extension>] static member isEmpty(xs:IEnumerable<'T>) = Seq.isEmpty xs
    //[<Obsolete("Net9.0 부터 지원되지 않는 형식: Length, Count, Count() 등 사용")>]
    //[<Extension>] static member length(xs:IEnumerable<'T>) = Seq.length xs
    //[<Obsolete("Net9.0 부터 지원되지 않는 형식: Any() 사용")>]
    //[<Extension>] static member any(xs:IEnumerable<'T>) = Seq.isEmpty xs |> not
    //[<Obsolete("Net9.0 부터 지원되지 않는 형식: Any() 사용")>]
    //[<Extension>] static member any(xs:IEnumerable<'T>, f) = Seq.tryFind f xs |> Option.isSome
    //[<Extension>] static member realize(xs:IEnumerable<'T>) = Array.ofSeq xs |> ignore

    //// } 임시 : 삭제 대상
