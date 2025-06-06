[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexTagManager with

    member v.CallPlanStartActive() =
        let v = v :?> CoinVertexTagManager
        let call = v.Vertex.GetPureCall()
       
        let dop, mop = v.Flow.d_st.Expr, v.Flow.mop.Expr
        let parentReal = call.Parent.GetCore() :?> Vertex
        let sets =
            (
                     (mop <&&> call.StartPointExpr)
                <||> (mop <&&> v.SFP.Expr)
                <||> (dop <&&> v.ST.Expr <&&> call.AutoPreExpr <&&> parentReal.V.G.Expr)
            )
            <&&> call.SafetyExpr

        let rstMemos = call.MutualResetCoins.Select(fun c->c.VC.PS)

        let sets = sets <&&> !@rstMemos.ToOrElseOff()
        let rsts = call.End <||> !@call.V.Flow.r_st.Expr// <||> parentReal.VR.RT.Expr

        
        let f = getFuncName()
        if call.Disabled then
            (v._off.Expr, rsts) ==| (v.PS, f)
        else
            (sets, rsts) ==| (v.PS, f)


    member v.CallPlanStartPassive() =
        let v = v :?> CoinVertexTagManager
        let call = v.Vertex.GetPureCall()
        let parentReal = call.Parent.GetCore() :?> Vertex
        let sets =  v.ST.Expr
        let rsts =  call.End <||> parentReal.VR.RT.Expr
        let f = getFuncName()
        (sets, rsts) ==| (v.PS, f)

    member v.C2_CallPlanEnd() =
        let v = v :?> CoinVertexTagManager
        let call = v.Vertex.GetPureCall()
        let sets = if call.IsJob 
                   then call.TargetJob.TaskDefs.Select(fun d->d.ApiItem.ApiItemEnd).ToAnd()
                   else v.PS.Expr

        let rsts = v._off.Expr
        (sets, rsts) --| (v.PE, getFuncName())

         
    member c.C3_InputDetected() =
        let _off = c.System._off.Expr
        match c.Vertex.GetPureCall().ActionInExpr with
        | Some inExprs -> [(inExprs, _off) --| (c.Vertex.VC.CallIn, getFuncName())]
        | None -> []

    member c.C4_OutputDetected() =
        let _off = c.System._off.Expr
        match c.Vertex.GetPureCall().ActionOutExpr with
        | Some outExprs -> [(outExprs, _off) --| (c.Vertex.VC.CallOut, getFuncName())]
        | None -> []

    member c.C5_StatActionFinish() =
        let _off = c.System._off.Expr
        let planStart = c.Vertex.VC.PE.Expr
        (planStart <&&> c.F.Expr, _off) --| (c.Vertex.VC.CalcStatActionFinish, getFuncName())
        //match c.Vertex.GetPureCall().ActionInExpr with
        //| Some inExprs -> (planStart <&&> inExprs, _off) --| (c.Vertex.VC.CalcStatActionFinish, getFuncName())
        //| None -> (planStart, _off)                      --| (c.Vertex.VC.CalcStatActionFinish, getFuncName())