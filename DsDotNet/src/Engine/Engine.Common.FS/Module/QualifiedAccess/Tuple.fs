namespace Engine.Common.FS

open Microsoft.FSharp.Reflection
open System.Collections.Generic

[<RequireQualifiedAccess>]
module Tuple =
    // https://stackoverflow.com/questions/2920094/how-can-i-convert-between-f-list-and-f-tuple
    let toSeq t = 
        if FSharpType.IsTuple(t.GetType()) 
        then FSharpValue.GetTupleFields t |> seq
        else Seq.empty

    let ofSeq sequ =
        let arr = sequ |> Array.ofSeq
        let types = arr |> Array.map (fun o -> o.GetType())
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

    /// tuple seq 를 쪼개서 각각의 seq 로 반환
    let flatten (xsys:seq<'x*'y>) = (xsys |> Seq.map fst, xsys |> Seq.map snd)

    // https://stackoverflow.com/questions/27924235/f-how-to-provide-fst-for-tuples-triples-and-quadruples-without-sacrifying-ru/27929600
    type Tuple1st = Tuple1st with
        static member ($) (Tuple1st, (x1,_)) = x1
        static member ($) (Tuple1st, (x1,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_)) = x1
        static member ($) (Tuple1st, (x1,_,_,_,_)) = x1
        // more overloads
    type Tuple2nd = Tuple2nd with
        static member ($) (Tuple2nd, (_,x1)) = x1
        static member ($) (Tuple2nd, (_,x1,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_)) = x1
        static member ($) (Tuple2nd, (_,x1,_,_,_)) = x1
        // more overloads
    type Tuple3rd = Tuple3rd with
        static member ($) (Tuple3rd, (_,_,x1)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_)) = x1
        static member ($) (Tuple3rd, (_,_,x1,_,_)) = x1
        // more overloads
    type Tuple4th = Tuple4th with
        static member ($) (Tuple4th, (_,_,_,x1)) = x1
        static member ($) (Tuple4th, (_,_,_,x1,_)) = x1
        // more overloads

    let inline tuple1st x = Tuple1st $ x
    let inline tuple2nd x = Tuple2nd $ x
    let inline tuple3rd x = Tuple3rd $ x
    let inline tuple4th x = Tuple4th $ x

    // https://stackoverflow.com/questions/14115260/extension-methods-for-f-tuples 도 참고... 동작하지 않는 듯..


