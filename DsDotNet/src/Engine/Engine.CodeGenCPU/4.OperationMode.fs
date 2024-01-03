[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with

    member f.O1_ReadyOperationState(): CommentedStatement =
        let set = f.ready_btn.Expr <||> f.HWBtnReadyExpr 
        let rst = f.eop.Expr <||> f.sop.Expr

        (set, rst) ==| (f.rop, getFuncName())

    member f.O2_AutoOperationState(): CommentedStatement =
        let set = f.AutoExpr 
        let rst = !!f.rop.Expr <||> f.ModeManualHwHMIExpr

        (set, rst) ==| (f.aop, getFuncName())

    member f.O3_ManualOperationState (): CommentedStatement =
        let set = f.ManuExpr
        let rst = !!f.rop.Expr <||> f.ModeAutoHwHMIExpr

        (set, rst) ==| (f.mop, getFuncName())


    member f.O4_EmergencyOperationState(): CommentedStatement =
        let set = f.emg_btn.Expr <||> f.HWBtnEmgExpr
        let rst = f._off.Expr

        (set, rst) --| (f.eop, getFuncName())

    member f.O5_StopOperationState(): CommentedStatement  =
        let setPause = (f.stop_btn.Expr <||> f.HWBtnStopExpr <||> f.sop.Expr) <&&> !!f.HWBtnClearExpr
        let setError = (f.Graph.Vertices.OfType<Real>().Select(getVM) 
                        |> Seq.collect(fun r-> [|r.E1; r.E2|])).ToOrElseOff(f.System)
        let set =
            if RuntimeDS.Package = LightPLC
            then
                setPause <||> setError
            else 
                setPause <||> (setError <&&> f.System._flicker1sec.Expr)

        let rst = f.clear_btn.Expr
        (set, rst) ==| (f.sop, getFuncName())

    member f.O6_DriveOperationMode (): CommentedStatement =
        let set = f.drive_btn.Expr <||> f.HWBtnDriveExpr 
        let rst = !!f.aop.Expr <||>  f.top.Expr

        (set, rst) ==| (f.dop, getFuncName())

    member f.O7_TestOperationMode (): CommentedStatement =
        let set = f.test_btn.Expr <||> f.HWBtnTestExpr
        let rst = !!f.aop.Expr <||> f.dop.Expr

        (set, rst) ==| (f.top, getFuncName())

    member f.O8_IdleOperationMode(): CommentedStatement =
        let set = !!(f.dop.Expr <||> f.top.Expr)
        let rst = f._off.Expr

        (set, rst) --| (f.iop, getFuncName())

