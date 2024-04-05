[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlowState

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with

    member f.ST1_originState() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.OG).ToAndElseOn()
        let rst = f._off.Expr

        (set, rst) --| (f.o_st, getFuncName())   

    member f.ST2_homingState() =
        let set =(f.home_btn.Expr <||> f.HWBtnHomeExpr ) <&&> (f.mop.Expr <||> f._sim.Expr)
        let rst = f._off.Expr

        (set, rst) --| (f.h_st, getFuncName())


    member f.ST3_goingState() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.G).ToOrElseOn()  <&&> f.d_st.Expr
        let rst = f.pause.Expr

        (set, rst) --| (f.g_st, getFuncName())

    member f.ST4_EmergencyState() =
        let set = f.emg_btn.Expr <||> f.HWBtnEmgExpr
        let rst = f.clear_btn.Expr

        (set, rst) --| (f.emg_st, getFuncName())

    member f.ST5_ErrorState() =
        let setDeviceError = (f.Graph.Vertices.OfType<Real>().Select(getVM) 
                                |> Seq.collect(fun r-> [|r.ErrTRX|])).ToOrElseOff()
        let setConditionError = f.stopConditionErr.Expr <&&> !!f._sim.Expr
        let set =  setDeviceError<||> setConditionError
        let rst = f.clear_btn.Expr
           
        (set, rst) ==| (f.e_st, getFuncName())

    member f.ST6_DriveState () =
        let set = (f.drive_btn.Expr <||> f.HWBtnDriveExpr)
        let rst = !!f.aop.Expr <||> f.t_st.Expr  <||> f.pause.Expr
         
        (set, rst) ==| (f.d_st, getFuncName())

    member f.ST7_TestState () =
        let set = f.test_btn.Expr <||> f.HWBtnTestExpr
        let rst = !!f.aop.Expr  <||> f.pause.Expr

        (set, rst) ==| (f.t_st, getFuncName())

    member f.ST8_ReadyState() =
        let set = f.ready_btn.Expr <||> f.HWBtnReadyExpr 
        ///시뮬레이션 때문에 e_st 사용 안하고  stopError/ stopConditionErr 구분해서사용
        let rst = (f.stopError.Expr <||> f.emg_st.Expr)
                  <||> (f.stopConditionErr.Expr <&&> !!f._sim.Expr)
                  <||> (f.pause.Expr)

        (set, rst) ==| (f.r_st, getFuncName())
