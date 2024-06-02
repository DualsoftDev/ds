[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo() =
        let v = v :?> VertexMCall
        let call = v.Vertex.GetPureCall().Value
       
        let dop, mop = v.Flow.d_st.Expr, v.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> v.ST.Expr)
                <||> (mop <&&> v.SF.Expr)
            )
            <&&> call.SafetyExpr
            
        let rst =
            if call.UsingTon 
            then
                (v.TDON.DN.Expr  <&&> dop)
                            <||>
                (call.End <&&> mop)
            else
                (call.EndPlan <&&> v._sim.Expr)
                            <||>
                (call.End <&&> !!v._sim.Expr)


        let parentReal = call.Parent.GetCore() :?> Vertex
        let rsts = rst <||> !!call.V.Flow.r_st.Expr <||> parentReal.VR.RT.Expr

        
        if call.Disabled 
        then
            (v._off.Expr, rsts) ==| (v.MM, getFuncName())
        else
            (sets, rsts) ==| (v.MM, getFuncName())

       
    




    