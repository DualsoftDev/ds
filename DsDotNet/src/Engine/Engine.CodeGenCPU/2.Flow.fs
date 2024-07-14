[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with

    member v.F1_RootStart() =
        let real = v.Vertex :?> Real
        let startCausals =  v.Vertex.GetStartRootAndCausals()
        let plans = v.System.GetApiSets(real).ToOrElseOff()
        let actionLinks = v.System.GetApiSensorLinks(real).ToOrElseOff()
        
        let shareds = v.Vertex.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.Vertex.GetStartRootAndCausals()).ToOrElseOn()
            else v._off.Expr

        let sets = (
                    (startCausals <||> wsShareds  <||> v.SF.Expr) <&&> v.Flow.d_st.Expr  
                    <||> plans 
                    <||> actionLinks 
                    )

        let rsts  = (real.V.RT.Expr <&&> real.CoinAlloffExpr)<||> real.V.F.Expr
        (sets, rsts) ==| (v.ST, getFuncName())//조건에 의한 릴레이


    member v.F2_RootReset()  =
        let real = v.Vertex.GetPureReal()
        let resetCausals =  v.Vertex.GetResetRootAndCausals()

        let shareds = v.Vertex.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.Vertex.GetResetRootAndCausals()).ToOrElseOn()
            else v._off.Expr

        let sets =  (
                        (resetCausals <||> wsShareds ) <&&> real.V.ET.Expr
                    ) 
                    <||> 
                    (
                        v.RF.Expr <||> (*real.VR.OB.Expr <||> *)real.VR.OA.Expr
                    )

        let rsts  = real.V.R.Expr
        (sets, rsts) ==| (v.RT, getFuncName())//조건에 의한 릴레이


    member v.F3_RealEndInFlow() =
        let sets =
            match v.Vertex  with
            | :? Alias   as rf -> rf.V.Vertex.GetPure().V.ET.Expr
            | _ ->
                failwithlog "Error"

        let rsts = v._off.Expr
        (sets, rsts) --| (v.ET, getFuncName())

    member v.F4_CallEndInFlow() =
        let sets =
            let callExpr = v.SF.Expr <&&> !@v.RF.Expr
            match v.Vertex  with
            | :? Call as call ->  
                if   call.IsOperator
                then
                     call.VC.CallOperatorValue.Expr  <||> callExpr
                else 
                     call.End  <||> callExpr
            | _ ->
                failwithlog "Error"
             
        (sets, v._off.Expr) --| (v.ET, getFuncName())

    member v.F5_HomeCommand() =
        let real = v.Vertex :?> Real
        (real.Flow.HomeExpr , v._off.Expr) --| (real.VR.OA, getFuncName())
        