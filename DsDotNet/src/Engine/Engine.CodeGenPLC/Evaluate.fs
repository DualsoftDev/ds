namespace Engine.Cpu

open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module EvaluateModule =

    //evaluation and/or
    let private evaluation(bits: (ICpuBit*bool) seq, orOperation:bool) =
        if bits |> Seq.isEmpty //평가 항목이 없으면 false
        then false
        else
             let items = bits |> Seq.map(fun (bit, negative) -> if negative then bit.Value|>not else bit.Value)
             if orOperation
             then items |> Seq.filter(fun v -> v = true)  |> Seq.isEmpty |> not //OR 평가 하나라도 ON 이면 TRUE
             else items |> Seq.filter(fun v -> v = false) |> Seq.isEmpty        //AND평가 전부     ON 이면 TRUE

    let EvaluationAnd(bits: (ICpuBit*bool) seq) = evaluation(bits, false)
    let EvaluationOr (bits: (ICpuBit*bool) seq) = evaluation(bits, true)




