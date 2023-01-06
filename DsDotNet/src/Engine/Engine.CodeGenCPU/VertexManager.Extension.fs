namespace Engine.CodeGenCPU

open System.Diagnostics
open System.Linq
open System.Text.RegularExpressions
open Engine.Core
open System.Runtime.CompilerServices

[<AutoOpen>]
module VertexManagerExtension =

    type VertexManager with
        ///Real 자신을 공용으로 사용하는 Vertex들  
        member v.GetSharedReal() : VertexManager seq =
                (v.Vertex :?> Real)
                    .GetVertexSharedReal()
                    .Select(fun v-> v.VertexManager)
                    .Cast<VertexManager>()

        ///Call 자신을 공용으로 사용하는 Vertex들  
        member v.GetSharedCall() : VertexManager seq =
                (v.Vertex :?> Call)
                    .GetVertexSharedCall()
                    .Select(fun v-> v.VertexManager)
                    .Cast<VertexManager>()


    
    let getPureCall(v:Vertex) =
            match v with
            | :? Call as c  ->  v :?> Call
            | :? Alias as a  -> a.TargetWrapper.GetTarget() :?> Call
            |_ -> failwith "Error"


    [<Extension>]
    type CodeConvertUtilExt =
        
        [<Extension>] static member STs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.ST) |> Seq.toList 
        [<Extension>] static member RTs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.RT) |> Seq.toList 
        [<Extension>] static member ETs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.ET) |> Seq.toList 
        [<Extension>] static member CRs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.CR) |> Seq.toList 
        [<Extension>] static member EmptyOnElseToAnd(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member EmptyOnElseToAnd(xs:DsBit seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:DsBit seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr


            
