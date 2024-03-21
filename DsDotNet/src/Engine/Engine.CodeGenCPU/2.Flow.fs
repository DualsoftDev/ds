[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with

    member v.F1_RootStart() =
        let real = v.Vertex :?> Real
        let startCausals =  v.GetWeakStartRootAndCausals()
        let plans = v.System.GetPSs(real).ToOrElseOff()
        let actionLinks = v.System.GetASs(real).ToOrElseOff()
        
        let shareds = v.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.GetWeakStartRootAndCausals()).ToOrElseOn()
            else v._off.Expr

        let sets = (startCausals <||> wsShareds <||> v.SF.Expr <||>  plans <||> actionLinks)
                   <&&> real.SafetyExpr

        let rsts  = real.V.RT.Expr <||> real.V.F.Expr
        [ (sets, rsts) ==| (v.ST, getFuncName()) ]//조건에 의한 릴레이


    member v.F2_RootReset()  =
        let real = v.GetPureReal()
        let resetCausals =  v.GetWeakResetRootAndCausals()

        let shareds = v.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.GetWeakResetRootAndCausals()).ToOrElseOn()
            else v._off.Expr

        let sets =  (
                    (resetCausals <||> wsShareds ) <&&> real.V.ET.Expr
                    ) 
                    <||> 
                    (
                    v.RF.Expr <||> (real.Flow.mop.Expr <&&> real.Flow.h_st.Expr)
                    )

        let rsts  = real.V.R.Expr
        [(sets, rsts) ==| (v.RT, getFuncName())]//조건에 의한 릴레이


    member v.F3_VertexEndWithOutReal() =
        let sets =
            match v.Vertex  with
            | :? Alias   as rf -> rf.V.GetPure().V.ET.Expr
            | :? RealExF as rf -> rf.V.GetPure().V.ET.Expr
            | :? Call as call ->
                if call.Parent.GetCore() :? Flow
                then call.EndAction 
                     <||> ( v._sim.Expr <&&> v.SF.Expr <&&> !!v.RF.Expr)
                else failwithlog $"Error this call Parent is flow but real {call.QualifiedName}" 
            | _ ->
                failwithlog "Error"
             


        let rsts = v._off.Expr
        (sets, rsts) --| (v.ET, getFuncName())

