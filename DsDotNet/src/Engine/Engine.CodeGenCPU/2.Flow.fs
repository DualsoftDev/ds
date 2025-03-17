[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexTagManager with

    member v.F1_RootStartActive() =
        let real = v.Vertex :?> Real
        let startCausals =  v.Vertex.GetStartRootAndCausals()
        let plans = v.System.GetApiSets(real).ToOrElseOff()
        let actionLinks = v.System.GetApiSensorLinks(real).ToOrElseOff()

        let shareds = v.Vertex.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.Any() then
                shareds.Select(fun s -> s.Vertex.GetStartRootAndCausals()).ToOrElseOn()
            else
                v._off.Expr
        let forceStart = v.SFP.Expr <&&> v.Flow.d_st.Expr
        let sets =
                ((startCausals <||> wsShareds  <||> forceStart) <&&> v.Flow.d_st.Expr)
                <||> plans
                <||> actionLinks

        let rsts = if real.Graph.Vertices.Any()
                    then (real.V.RT.Expr <&&> real.CoinAlloffExpr)<||> real.V.F.Expr
                    else real.V.RT.Expr <||> real.V.F.Expr

        (sets, rsts) ==| (v.ST, getFuncName())//조건에 의한 릴레이

        
    member v.F1_RootStartPassive() =
        let real = v.Vertex :?> Real
        let plans = v.System.GetApiSets(real).ToOrElseOff()
       
        let sets = (
                    v.SFP.Expr <||> plans <||>
                    real.Graph.Inits.Select(fun v->v.VC.CallOut)
                       .ToOrElseOff()
                    )
                   <&&> v.Flow.d_st.Expr

        let rsts = if real.Graph.Vertices.Any()
                    then real.V.F.Expr <||> (real.V.RT.Expr <&&> real.CoinAlloffExpr)
                    else real.V.F.Expr <||>  real.V.RT.Expr 

        (sets , rsts) ==| (v.ST, getFuncName())//조건에 의한 릴레이

        

    member v.F2_RootResetActive()  =
        let real = v.Vertex.GetPureReal()
        let resetCausals =  v.Vertex.GetResetRootAndCausals()

        let shareds = v.Vertex.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.Any() then
                shareds.Select(fun s -> s.Vertex.GetResetRootAndCausals()).ToOrElseOn()
            else
                v._off.Expr
        let manualReset = v.RFP.Expr <&&> v.Flow.mop.Expr
        
                    //if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() ahn!!
                    //        then  v.RFP.Expr
                    //        else  v.RFP.Expr <&&> v.Flow.mop.Expr
        let sets =
            ( (resetCausals <||> wsShareds ) <&&> real.V.ET.Expr)
            <||>
            ( manualReset)
             <||>
            (( real.VR.OB.Expr <||>  real.VR.OA.Expr ) <&&> real.Flow.mop.Expr <&&> !@v.Vertex.VR.OG.Expr)

        let rsts = real.V.R.Expr
        (sets, rsts) ==| (v.RT, getFuncName())//조건에 의한 릴레이

    member v.F2_RootResetPassive()  =
        let real = v.Vertex :?> Real
       
        let sets = v.RFP.Expr <||>
                    if real.Graph.Vertices.Any()
                    then real.CoinETContacts.ToAnd()   
                    else v._off.Expr

        let rsts = real.V.R.Expr
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
            let forceStart = v.SFP.Expr <&&> v.Flow.d_st.Expr
            let callExpr = forceStart <&&> !@v.RFP.Expr
            let getExpr(call:Call) =
                if call.IsOperator then
                    call.VC.CallOperatorValue.Expr  <||> callExpr
                else
                    call.End  <||> callExpr

            match v.Vertex  with
            | :? Call as c ->   getExpr c
            | :? Alias as rf ->
                match  rf.TargetWrapper with
                | DuAliasTargetReal _ -> failwithlog $"Error {getFuncName()} : {v.Vertex.QualifiedName}"
                | DuAliasTargetCall c -> getExpr c
            | _ ->
                failwithlog $"Error {getFuncName()} : {v.Vertex.QualifiedName}"

        (sets, v._off.Expr) --| (v.ET, getFuncName())



    member v.F7_HomeCommand() =
        let real = v.Vertex :?> Real
        (real.Flow.HomeExpr <&&> real.Flow.mop.Expr , v._off.Expr) --| (real.VR.OA, getFuncName())
