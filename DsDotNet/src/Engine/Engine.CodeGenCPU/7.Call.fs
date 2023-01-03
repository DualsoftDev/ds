[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core

let getPureCall(v:Vertex) =
            match v with
            | :? Call as c  ->  v :?> Call
            | :? Alias as a  -> a.TargetWrapper.GetTarget() :?> Call
            |_ -> failwith "Error"

type VertexManager with

    member v.C1_CallActionOut(): CommentedStatement list = 
        let call = v.Vertex :?> Call
        
        let sets = v.ST.Expr <||> v.GetSharedCall().Select(fun s->s.ST).ToOr()
        let rsts = if call.MutualResetOuts.Any() then call.MutualResetOuts.ToOr() else v.System._off.Expr
        [
            for out in call.OUTs do
                yield (sets, rsts) --| (out, "C1" )
        ]

    member v.C2_CallHeadComplete(): CommentedStatement  = 
        let call = getPureCall  v.Vertex
        let realV = call.Parent.GetCore() :?> Real |> getVM
        
        let sets = call.INs.ToAnd() <&&> realV.RR.Expr  <&&> v.ET.Expr
        let rsts = realV.H.Expr
        (sets, rsts) ==| (v.CR, "C2")


    member v.C3_CallTailComplete(): CommentedStatement  = 
        let call = getPureCall  v.Vertex
        let real = call.Parent.GetCore() :?> Real
        let realV = real |> getVM
        let srcCausal = real.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM).Select(fun f->f.CR)
        
        let sets = call.INs.ToAnd() <&&> srcCausal.ToAnd()  <&&> v.ET.Expr
        let rsts = realV.H.Expr
        (sets, rsts) ==| (v.CR, "C3")
                 
    member v.C4_CallTx(): CommentedStatement list = 
        let call = getPureCall  v.Vertex
        let sets = v.ST.Expr <||> v.GetSharedCall().Select(fun s->s.ST).ToOr()
        let rsts = v.System._off.Expr
        [
            for out in call.TXs do
                yield (sets, rsts) --| (out, "C4" )
        ]

    member v.C5_CallRx(): CommentedStatement  = 
        let call = getPureCall  v.Vertex
        let sets = call.RXs.ToAnd()
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.ET, "C5" )
    