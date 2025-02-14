namespace Dual.Common.Core.FS

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open Microsoft.FSharp.Reflection


[<RequireQualifiedAccess>]
module Tuple =
    // https://stackoverflow.com/questions/2920094/how-can-i-convert-between-f-list-and-f-tuple
    let toSeq t =
        if FSharpType.IsTuple(t.GetType())
        then FSharpValue.GetTupleFields t |> seq
        else Seq.empty

    /// Seq 로부터 Tuple 생성
    ///
    /// 반드시 obj seq 이어야 한다.
    /// e.g [box 1; "Hello"] |> Tuple.ofSeq => (1, "Hello")
    let ofSeq (ts:obj seq) =
        let arr = ts |> Array.ofSeq
        let types = arr |> Array.map _.GetType()
        let tupleType = FSharpType.MakeTupleType types
        FSharpValue.MakeTuple (arr, tupleType)


    (* Active pattern : Operators.KeyValue 도 참고
        let dic = [(1, "One"); ] |> dict |> Dictionary
        dic |> Seq.map (fun (KeyValue(k, v) -> ... )
     *)
    let ofKeyValuePair (kv:KeyValuePair<'k, 'v>) = (kv.Key, kv.Value)
    let toKeyValuePair (k,v) = KeyValuePair(k, v)

    let swap (a, b) = (b, a)

    /// tuple seq 를 dicitionary 로 변환
    let toDictionary tpl = tpl |> dict |> Dictionary

    /// tuple seq 를 dicitionary 로 변환 : F# readOnlyDict (vs dict)
    let toReadOnlyDictionary tpl = tpl |> dict |> ReadOnlyDictionary

    /// tuple seq 를 쪼개서 각각의 seq 로 반환
    let flatten (xsys:seq<'x*'y>) = (xsys |> Seq.map fst, xsys |> Seq.map snd)

    // https://stackoverflow.com/questions/27924235/f-how-to-provide-fst-for-tuples-triples-and-quadruples-without-sacrifying-ru/27929600
    type Tuple1st = Tuple1st with
        static member ($) (Tuple1st, (x1,_)) = x1
        static member ($) (Tuple1st, (x1,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_,_,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_,_,_,_,_)) = x1
    type Tuple2nd = Tuple2nd with
        static member ($) (Tuple2nd, (_,x1)) = x1
        static member ($) (Tuple2nd, (_,x1,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_,_,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_,_,_,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_,_,_,_,_)) = x1
    type Tuple3rd = Tuple3rd with
        static member ($) (Tuple3rd, (_,_,x1)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_,_)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_,_,_)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_,_,_,_)) = x1
    type Tuple4th = Tuple4th with
        static member ($) (Tuple4th, (_,_,_,x1)) = x1
        static member ($) (Tuple4th, (_,_,_,x1,_)) = x1
        static member ($) (Tuple4th, (_,_,_,x1,_,_)) = x1
        static member ($) (Tuple4th, (_,_,_,x1,_,_,_)) = x1
    type Tuple5th = Tuple5th with
        static member ($) (Tuple5th, (_,_,_,_,x1)) = x1
        static member ($) (Tuple5th, (_,_,_,_,x1,_)) = x1
        static member ($) (Tuple5th, (_,_,_,_,x1,_,_)) = x1
    type Tuple6th = Tuple6th with
        static member ($) (Tuple6th, (_,_,_,_,_,x1)) = x1
        static member ($) (Tuple6th, (_,_,_,_,_,x1,_)) = x1
    type Tuple7th = Tuple7th with
        static member ($) (Tuple7th, (_,_,_,_,_,_,x1)) = x1


    type TupleLast = TupleLast with
        static member ($) (TupleLast, (_,x1)) = x1
        static member ($) (TupleLast, (_,_,x1)) = x1
        static member ($) (TupleLast, (_,_,_,x1)) = x1
        static member ($) (TupleLast, (_,_,_,_,x1)) = x1
        static member ($) (TupleLast, (_,_,_,_,_,x1)) = x1
        static member ($) (TupleLast, (_,_,_,_,_,_,x1)) = x1
        static member ($) (TupleLast, (_,_,_,_,_,_,_,x1)) = x1



    /// 임의 size(<=7) tuple 에서 1st element 반환
    [<Obsolete("Use Tuple.first")>]
    let inline tuple1st x = Tuple1st $ x
    /// 임의 size(<=7) tuple 에서 2nd element 반환
    [<Obsolete("Use Tuple.second")>]
    let inline tuple2nd x = Tuple2nd $ x
    /// 임의 size(<=7) tuple 에서 3rd element 반환
    [<Obsolete("Use Tuple.third")>]
    let inline tuple3rd x = Tuple3rd $ x
    /// 임의 size(<=7) tuple 에서 4-th element 반환
    [<Obsolete("Use Tuple.fourth")>]
    let inline tuple4th x = Tuple4th $ x

    /// 임의 size(<=7) tuple 에서 1st element 반환
    let inline first  x = Tuple1st $ x
    /// 임의 size(<=7) tuple 에서 2nd element 반환
    let inline second x = Tuple2nd $ x
    /// 임의 size(<=7) tuple 에서 3rd element 반환
    let inline third  x = Tuple3rd $ x
    /// 임의 size(<=7) tuple 에서 4-th element 반환
    let inline fourth x = Tuple4th $ x
    let inline fifth  x = Tuple5th $ x
    let inline sixth  x = Tuple6th $ x
    let inline seventh x = Tuple7th $ x

    /// 임의 size(<=7) tuple 에서 마지막 element 반환
    let inline last x = TupleLast $ x

    /// 임의 size(<=7) tuple 에서 n-th element 반환
    /// zero based index
    let item n t =
        if FSharpType.IsTuple(t.GetType()) then
            let tupleFields = FSharpValue.GetTupleFields(t)
            if n < 0 || n >= tupleFields.Length then
                failwithf "Tuple.nth: %d is out of range" n
            else
                tupleFields.[n]
        else
            failwith "The input value is not a tuple"

    // https://stackoverflow.com/questions/14115260/extension-methods-for-f-tuples 도 참고... 동작하지 않는 듯..

    [<Obsolete("Use item instead")>]
    let nth n t = item n t



