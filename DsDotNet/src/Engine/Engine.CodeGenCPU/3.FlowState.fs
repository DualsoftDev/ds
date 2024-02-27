[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlowState

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with



    member f.O4_EmergencyOperationState() =
        let set = f.emg_btn.Expr <||> f.HWBtnEmgExpr
        let rst = f.clear_btn.Expr

        (set, rst) ==| (f.emg_st, getFuncName())

    member f.O5_StopOperationState() =
        let setDeviceError = (f.Graph.Vertices.OfType<Real>().Select(getVM) 
                                |> Seq.collect(fun r-> [|r.ErrTRX|])).ToOrElseOff()
        let setConditionError = f.stopConditionErr.Expr <&&> !!f._sim.Expr
        let set =  setDeviceError<||> setConditionError
        let rst = f.clear_btn.Expr
           
        (set, rst) ==| (f.e_st, getFuncName())

    member f.O6_DriveOperationMode () =
        let set = f.drive_btn.Expr <||> f.HWBtnDriveExpr    
        let rst = !!f.aop.Expr <||>  f.t_st.Expr

        (set, rst) ==| (f.d_st, getFuncName())

    member f.O7_TestOperationMode () =
        let set = f.test_btn.Expr <||> f.HWBtnTestExpr
        let rst = !!f.aop.Expr <||> f.d_st.Expr

        (set, rst) ==| (f.t_st, getFuncName())

    member f.O8_ReadyOperationState() =
        let set = f.ready_btn.Expr <||> f.HWBtnReadyExpr 
        let rst = (f.stopError.Expr <||> f.emg_st.Expr) <||> (f.stopConditionErr.Expr <&&> !!f._sim.Expr)

        (set, rst) ==| (f.r_st, getFuncName())

    member f.O9_originOperationMode() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.OG).ToAndElseOn()
        let rst = f._off.Expr

        (set, rst) --| (f.o_st, getFuncName())   

    member f.O10_homingOperationMode() =
        let set = f.home_btn.Expr <||> f.HWBtnHomeExpr
        let rst = f._off.Expr

        (set, rst) --| (f.h_st, getFuncName())


    member f.O11_goingOperationMode() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.G).ToOrElseOn()  <&&> f.d_st.Expr
        let rst = f._off.Expr

        (set, rst) --| (f.g_st, getFuncName())

