[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement list =
        let real = v.Vertex :?> Real
        let wsDirect =  v.GetWeakStartRootAndCausals()
        let ssDirect =  v.GetStrongStartRootAndCausals()
        let planSets = v.System.GetPSs(real).ToOrElseOff(v.System)

        let shareds = v.GetSharedReal().Select(getVM)
        let wsShareds =
            if shareds.any()
            then shareds.Select(fun s -> s.GetWeakStartRootAndCausals()).ToOr()
            else v._off.Expr

        let sets = wsDirect <||> wsShareds <||> v.SF.Expr <||> ssDirect <||> planSets
        let rsts  = real.V.RT.Expr <||> real.V.F.Expr
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

        let sets =  (
                    (wrDirect <||> wsShareds  <||> srDirect<||> v.RT.Expr)
                    <&&> real.V.ET.Expr
                    ) 
                    <||> v.RF.Expr 
        let rsts = v._off.Expr 
        [(sets, rsts) --| (v.RT, getFuncName())] //reset tag


    member v.F3_VertexEndWithOutReal() : CommentedStatement  =
        let sets =
            match v.Vertex  with
            | :? Alias   as rf -> rf.V.GetPure().V.ET.Expr
            | :? RealExF as rf -> rf.V.GetPure().V.ET.Expr
            | :? Call as call ->
                let action =
                    if call.UsingTon
                            then call.V.TDON.DN.Expr   //On Delay
                            else call.INsFuns
                  
                (action <||> v._sim.Expr)
            | _ ->
                failwithlog "Error"

        let rsts = v._off.Expr
        (sets, rsts) --| (v.ET, getFuncName())

