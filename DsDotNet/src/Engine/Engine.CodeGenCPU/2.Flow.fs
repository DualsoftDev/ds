[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let wsDirect =  v.GetWeakStartRootAndCausals()
        let ssDirect =  v.GetStrongStartRootAndCausals()

        let shareds = v.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.GetWeakStartRootAndCausals()).ToOr()
            else v._off.Expr

        let sets = wsDirect <||> wsShareds <||> v.SF.Expr <||> ssDirect
        let rsts  = real.V.RP.Expr <||> real.V.F.Expr
        [ (sets, rsts) ==| (v.ST, getFuncName()) ]


    member v.F2_RootReset() : CommentedStatement list=
        let real = v.GetPureReal()
        let wrDirect =  v.GetWeakResetRootAndCausals()
        let srDirect =  v.GetStrongResetRootAndCausals()

        let shareds = v.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.GetWeakResetRootAndCausals()).ToOr()
            else v._off.Expr

        let sets = wrDirect <||> wsShareds  <||> v.RF.Expr <||> srDirect
        let rsts = (!!)real.V.ET.Expr
        [(sets, rsts) ==| (v.RT, getFuncName())] //reset tag

    member v.F3_RootGoingPulse() : CommentedStatement  =
        let real = v.GetPureReal()
        (real.V.G.Expr, v._off.Expr) --^ (real.V.GPUL, "RootGoingPulse")


    member v.F4_RootGoingRelay() : CommentedStatement list =
        let real = v.GetPureReal()

        let shareds = v.GetSharedReal().Select(getVM)
        let sets =
            shareds @ [v]
            |>Seq.collect(fun r ->
                getResetWeakEdgeSources(r)
                |>Seq.map(fun s -> s.V.GetPureReal().V, [s].GetResetWeakCausals(r).Head())
            )

        sets
        |> Seq.map(fun (src, gr) ->(src.GPUL.Expr <&&> real.V.F.Expr, real.V.H.Expr) ==| (gr, "RootGoingRelay"))
        |> Seq.toList

    member v.F5_RootCoinRelay() : CommentedStatement =
        let v = v :?> VertexMCoin
        let ands =
            match v.Vertex  with
            | :? RealExF as rf -> rf.V.CR.Expr
            | :? CallSys as rs -> rs.V.CR.Expr
            | :? CallDev | :? Alias ->
                match v.GetPureCall() with
                | Some call ->  if call.UsingTon
                                then call.V.TON.DN |> var2expr
                                else call.INs.ToAndElseOn(v.System)
                | None -> v.CR.Expr
            | _ ->
                failwithlog $"Error"

        let sets = ands <&&> v.ET.Expr
        let rsts = !!v.ST.Expr
        (sets, rsts) ==| (v.CR, getFuncName())

    //option Spec 확정 필요
     //member v.F0_RootStartRealOptionPulse(): CommentedStatement list =
     //   let rsts  = v.F.Expr
     //   [
     //       let ws =  getStartWeakEdgeSources(v.Flow.Graph, v.Vertex)
     //       let sets = ws.GetCausalTags(v.System, true)
     //               //root 시작조건 이벤트 Pulse 처리
     //       yield (sets, rsts) --^ (v.PUL, getFuncName() )
     //       yield (v.PUL.Expr, v.H.Expr) ==| (v.ST, getFuncName())
     //   ]
