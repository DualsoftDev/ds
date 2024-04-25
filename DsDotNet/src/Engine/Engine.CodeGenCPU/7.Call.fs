[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo() =
        let v = v :?> VertexMCall
        let call = 
            match v.Vertex with
            | :? Call as c->  c
            | :? Alias as al->  al.TargetWrapper.CallTarget().Value
            |_ -> failwithf "error coin Type"


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
                (call.EndActionOnlyIO <&&> mop)
            else
                (call.EndPlan <&&> v._sim.Expr)
                            <||>
                (call.EndAction <&&> !!v._sim.Expr)


        let parentReal = call.Parent.GetCore() :?> Vertex
        let rsts = rst <||> !!call.V.Flow.r_st.Expr <||> parentReal.VR.RT.Expr
        (sets, rsts) ==| (v.MM, getFuncName())

    
    member v.C2_ActionOut() =
        let v = v :?> VertexMCall
        let coin = v.Vertex :?> Call
        [
            let rstNormal = coin._off.Expr
            for td in coin.TargetJob.DeviceDefs do
                let api = td.ApiItem
                if td.OutAddress <> TextSkip && td.OutAddress <> TextAddrEmpty
                then 
                    let rstMemos = coin.MutualResetCalls.Select(fun c->c.VC.MM)
                    let sets =
                        if RuntimeDS.Package.IsPackageEmulation()
                        then api.PE.Expr <&&> api.PS.Expr <&&> coin._off.Expr
                        else api.PE.Expr <&&> api.PS.Expr <&&> !!rstMemos.ToOrElseOff()

                    if coin.TargetJob.ActionType = JobActionType.Push 
                    then 
                            let rstPush = rstMemos.ToOr()
                        
                            yield (sets, rstPush  ) ==| (td.AO, getFuncName())
                    else 
                            yield (sets, rstNormal) --| (td.AO, getFuncName())
        ]


    member v.C3_FunctionOut() =
        let v = v :?> VertexMCall
        let coin = v.Vertex :?> Call
        [
            let set = v.PSFunc.Expr
            match coin.CallCommandType with 
            | DuCMDAdd -> yield set --+ (v.PSFunc, getFuncName())
            | DuCMDSub -> yield set --- (v.PSFunc, getFuncName())
            | DuCMDMove -> yield set --> (v.PSFunc, getFuncName())
            | _-> failwithlog $"{v.Name} 함수 정의가 없습니다."
        ]
