[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System
open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexTagManager with

    member v.F1_RootStart() =
        let real = v.Vertex :?> Real
        let startCausals =  v.Vertex.GetStartRootAndCausals()
        let plans = v.System.GetApiSets(real).ToOrElseOff()
        let actionLinks = v.System.GetApiSensorLinks(real).ToOrElseOff()

        let shareds = v.Vertex.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any() then
                shareds.Select(fun s -> s.Vertex.GetStartRootAndCausals()).ToOrElseOn()
            else
                v._off.Expr

        let sets =
                ((startCausals <||> wsShareds  <||> v.SF.Expr) <&&> v.Flow.d_st.Expr)
                <||> plans
                <||> actionLinks

        let rsts = (real.V.RT.Expr <&&> real.CoinAlloffExpr)<||> real.V.F.Expr
        (sets, rsts) ==| (v.ST, getFuncName())//조건에 의한 릴레이


    member v.F2_RootReset()  =
        let real = v.Vertex.GetPureReal()
        let resetCausals =  v.Vertex.GetResetRootAndCausals()

        let shareds = v.Vertex.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any() then
                shareds.Select(fun s -> s.Vertex.GetResetRootAndCausals()).ToOrElseOn()
            else
                v._off.Expr

        let sets =
            ( (resetCausals <||> wsShareds ) <&&> real.V.ET.Expr )
            <||>
            ( v.RF.Expr <||> (*real.VR.OB.Expr <||> *)real.VR.OA.Expr )

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
            let callExpr = v.SF.Expr <&&> !@v.RF.Expr
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
                | DuAliasTargetCall c ->    getExpr c
            | _ ->
                failwithlog $"Error {getFuncName()} : {v.Vertex.QualifiedName}"

        (sets, v._off.Expr) --| (v.ET, getFuncName())




    member v.F5_SourceTokenNumGeneration() =
        let vc = getVMCall v.Vertex
        let fn = getFuncName()
        match v.Vertex.TokenSourceOrder with
        | Some order ->
            [|
                let tempInit= v.System.GetTempBoolTag("tempInitCheckTokenSrc")
                let initExpr = 0u|>literal2expr ==@ vc.SourceTokenData.ToExpression()
                yield (initExpr, v._off.Expr) --| (tempInit, fn)

                let totalSrcToken = v.System.GetSourceTokenCount()

                if RuntimeDS.Package.IsPLCorPLCSIM()
                then
                    //처음에는 자기 순서로 시작
                    yield (fbRising[tempInit.Expr]   <&&> v.ET.Expr ,        order|>uint32|>literal2expr) --> (vc.SourceTokenData, fn)
                    //이후부터는 전체 값 만큼 증가
                    yield (fbRising[!@tempInit.Expr] <&&> v.ET.Expr, totalSrcToken|>uint32|>literal2expr, vc.SourceTokenData.ToExpression()) --+ (vc.SourceTokenData, fn)
                else 
                    yield (tempInit.Expr   <&&> v.ET.Expr ,        order|>uint32|>literal2expr) --> (vc.SourceTokenData, fn)
                    yield (!@tempInit.Expr <&&> v.ET.Expr, totalSrcToken|>uint32|>literal2expr, vc.SourceTokenData.ToExpression()) --+ (vc.SourceTokenData, fn)
            |]
        |None -> [||]


    member v.F7_HomeCommand() =
        let real = v.Vertex :?> Real
        (real.Flow.HomeExpr <&&> real.Flow.mop.Expr , v._off.Expr) --| (real.VR.OA, getFuncName())
