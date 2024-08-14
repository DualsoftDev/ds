[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlowState

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with

    member f.ST1_OriginState() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVMReal)
                   .Select(fun r-> r.OG).ToAndElseOn()
        let rst = f._off.Expr

        (set, rst) --| (f.o_st, getFuncName())   

        
    member f.ST2_ReadyState() =  //f.driveCondition.Expr  는 수동 운전해야 해서 에러는 아님
        let set = f.ReadyExpr <&&> f.readyCondition.Expr
        let rst = f.e_st.Expr <||> f.emg_st.Expr <||> f.p_st.Expr
        (set, rst) ==| (f.r_st, getFuncName())

    member f.ST3_GoingState() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.G).ToOrElseOn()  <&&> f.d_st.Expr
        let rst = f.p_st.Expr

        (set, rst) --| (f.g_st, getFuncName())

    member f.ST4_EmergencyState() =
        let set = f.EmgExpr
        let rst = f.ClearExpr

        (set, rst) --| (f.emg_st, getFuncName())

    member f.ST5_ErrorState() =
        let setDeviceError = (f.Graph.Vertices.OfType<Real>().Select(getVMReal) 
                                |> Seq.collect(fun r-> [|r.ErrTRX|])).ToOrElseOff()
        let setConditionError = !@f.readyCondition.Expr <&&> f.r_st.Expr //f.driveCondition.Expr  는 수동 운전해야 해서 에러는 아님
        let set =  setDeviceError<||> setConditionError
        let rst = f.ClearExpr
           
        (set, rst) ==| (f.e_st, getFuncName())

    member f.ST6_DriveState () =
        let set = f.DriveExpr <&&> f.driveCondition.Expr
        let rst = !@f.aop.Expr <||> f.t_st.Expr  <||> f.p_st.Expr
        (set, rst) ==| (f.d_st, getFuncName())


    member f.ST7_TestState () =
        let set = f.TestExpr
        let rst = !@f.aop.Expr  <||> f.p_st.Expr

        (set, rst) ==| (f.t_st, getFuncName())
