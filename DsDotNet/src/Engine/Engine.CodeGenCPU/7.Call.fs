[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexMCoin with
    member coin.C1_CallPlanSend(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let dop, mop = coin.Flow.dop.Expr, coin.Flow.mop.Expr
        let sharedCalls = coin.GetSharedCall().Select(getVM)
        let startTags   = ([coin.ST] @ sharedCalls.STs()).ToOr()
        let forceStarts = ([coin.SF] @ sharedCalls.SFs()).ToOr()

                          
        let interlockPE (td:TaskDev) = if td.ApiItem.RXs.any() then  td.ApiItem.PE.Expr else coin._off.Expr
        let getStartPointExpr(coin:CallDev, td:TaskDev) =
            match coin.Parent.GetCore() with
            | :? Real as r ->
                let tasks = r.V.OriginInfo.Tasks
                if tasks.Where(fun (_,ty) -> ty = InitialType.On) //NeedCheck 처리 필요 test ahn
                        .Select(fun (t,_)->t).Contains(td)
                    then r.V.RO.Expr <&&> if td.ExistIn then !!td.ActionINFunc else call._on.Expr
                                             
                    else r.V.RO.Expr <&&> call._off.Expr
            | _ -> 
                call._off.Expr

        [
            for td in call.CallTargetJob.DeviceDefs do
                let sets = (dop <&&> startTags <||> getStartPointExpr (call, td)) <||>
                           (mop <&&> forceStarts) 
                           <&&>
                           !!td.MutualReset(coin.System).Select(fun f -> f.ApiItem.PS)
                               .ToAndElseOff(coin.System)
                           <&&>
                           !!(interlockPE td)

                //let rsts = (dop <&&> coin.CR.Expr)
                //           <||> (mop  <&&> coin.ET.Expr)

                yield (sets, coin.ET.Expr) ==| (td.ApiItem.PS, getFuncName())
        ]


    member coin.C2_CallActionOut(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let rsts = coin._off.Expr
        [
            for td in call.CallTargetJob.DeviceDefs do
                if td.ApiItem.TXs.any()
                then yield (td.ApiItem.PS.Expr, rsts) --| (td.ActionOut, getFuncName())
        ]

  
    member coin.C3_CallPlanReceive(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        [
            for td in call.CallTargetJob.DeviceDefs do

                let sets =  td.RXTags.ToAndElseOn(coin.System) 

                yield (sets, coin._off.Expr) --| (td.ApiItem.PE, getFuncName() )
        ]

    member coin.C4_5_CallActionIn(bRoot:bool): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let sharedCalls = coin.GetSharedCall() @ [coin.Vertex]
        
        let rsts = coin._off.Expr
        [
            for sharedCall in sharedCalls do
                let sets =
                    let action =
                        if call.UsingTon
                            then call.V.TON.DN.Expr   //On Delay
                            else call.INsFuns
                             
                  
                    (action <||> coin._sim.Expr)
                    <&&> call.PSs.ToAndElseOn(coin.System) 
                    <&&> if bRoot then coin._on.Expr
                                  else call.PEs.ToAndElseOn(coin.System) 

                yield (sets, rsts) --| (sharedCall.V.ET, getFuncName() )
        ]

   


type VertexManager with
    member v.C1_CallPlanSend()       : CommentedStatement list = (v :?> VertexMCoin).C1_CallPlanSend()
    member v.C2_CallActionOut()      : CommentedStatement list = (v :?> VertexMCoin).C2_CallActionOut()
    member v.C3_CallPlanReceive()    : CommentedStatement list = (v :?> VertexMCoin).C3_CallPlanReceive()
    member v.C4_CallActionIn()       : CommentedStatement list = (v :?> VertexMCoin).C4_5_CallActionIn(false)
    member v.C5_CallActionInRoot()   : CommentedStatement list = (v :?> VertexMCoin).C4_5_CallActionIn(true)
