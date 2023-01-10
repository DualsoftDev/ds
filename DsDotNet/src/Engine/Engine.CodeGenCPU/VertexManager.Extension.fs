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
                (v.Vertex :?> Real).GetVertexSharedReal().Select(getVM) 

            ///Call 자신을 공용으로 사용하는 Vertex들  
        member v.GetSharedCall() : VertexManager seq =
                (v.Vertex :?> Call).GetVertexSharedCall().Select(getVM) 
           
           ///Call 자신이거나 Alias Target Call
        member v.GetPureCall() : Call option=
                match v.Vertex with
                | :? Call as c  ->  Some (c) 
                | :? Alias as a  ->
                        if a.TargetWrapper.GetTarget() :? Call then 
                            Some (a.TargetWrapper.GetTarget() :?> Call)
                        else None
                |_ -> None
           
           ///Real 자신이거나 RealEx Target Real
        member v.GetPureReal() : Real =
                match v.Vertex with
                | :? Real as r  ->  r
                | :? RealEx as re  -> re.Real
                |_ -> failwith "Error GetTargetCall"

    [<Extension>]
    type CodeConvertUtilExt =
        
        [<Extension>] static member STs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.ST) |> Seq.toList 
        [<Extension>] static member RTs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.RT) |> Seq.toList 
        [<Extension>] static member ETs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.ET) |> Seq.toList 
        [<Extension>] static member CRs(xs:VertexManager seq): DsBit list = xs.Select(fun s->s.CR) |> Seq.toList 
        [<Extension>] static member EmptyOnElseToAnd(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOnElseToAnd(xs:DsBit seq, sys:DsSystem) = if xs.Any() then xs.ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOnElseToAnd(xs:DsTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.Cast<Tag<bool>>().ToAnd() else sys._on.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:PlcTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:DsBit seq, sys:DsSystem) = if xs.Any() then xs.ToOr() else sys._off.Expr
        [<Extension>] static member EmptyOffElseToOr(xs:DsTag<bool> seq, sys:DsSystem) = if xs.Any() then xs.Cast<Tag<bool>>().ToOr() else sys._off.Expr


            
