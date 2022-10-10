namespace Engine.CodeGen

open Engine.Core
open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module EvaluateModule =
     
    //평가항목이 없거나 하나라도 false 이면 return false
    let Evaluation(bits: (Bit*bool) seq) =
        if bits |> Seq.isEmpty
        then false
        else bits
             |> Seq.map(fun (bit, inv) -> if inv then bit.Value|>not else bit.Value)
             |> Seq.filter(fun value -> value = false)
             |> Seq.isEmpty |> not



