[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type Flow with

    member f.O1_ReadyOperationState(): CommentedStatement =
        let set = f.ready.Expr <||> f.BtnReadyExpr
        let rst = !!f.scr.Expr <||> f.eop.Expr <||> f.sop.Expr

        (set, rst) ==| (f.rop, "O1")

    member f.O2_AutoOperationState(): CommentedStatement =
        let set = f.ModeAutoHwExpr// <&&> f.ModeAutoSwHMIExpr  //test ahn lightPLC 모드 준비중
        let rst = !!f.rop.Expr <||> f.ModeManualHwExpr

        (set, rst) ==| (f.aop, "O2")

    member f.O3_ManualOperationState (): CommentedStatement =
        let set = f.ModeManualHwExpr// <||> f.ModeManualSwHMIExpr
        let rst = !!f.rop.Expr <||> f.ModeAutoHwExpr

        (set, rst) ==| (f.mop, "O3")


    member f.O4_EmergencyOperationState(): CommentedStatement =
        let set = f.emg.Expr <||> f.BtnEmgExpr
        let rst = f._off.Expr

        (set, rst) --| (f.eop, "O4")

    member f.O5_StopOperationState(): CommentedStatement =
        let set = f.stop.Expr <||> f.BtnStopExpr
        let setErrs = f.GetVerticesWithInReal().Select(getVM).ERRs().ToOrElseOff(f.System)
        let rst = f.clear.Expr

        (set <||> setErrs, rst) --| (f.sop, "O5")

    member f.O6_DriveOperationMode (): CommentedStatement =
        let set = f.drive.Expr <||> f.BtnDriveExpr
        let rst = !!f.aop.Expr <||> !!f.scd.Expr  <||> f.top.Expr

        (set, rst) ==| (f.dop, "O6")

    member f.O7_TestOperationMode (): CommentedStatement =
        let set = f.test.Expr <||> f.BtnTestExpr
        let rst = !!f.aop.Expr <||> !!f.scd.Expr  <||> f.dop.Expr

        (set, rst) ==| (f.top, "O7")

    member f.O8_IdleOperationMode(): CommentedStatement =
        let set = !!(f.drive.Expr <||> f.test.Expr)
        let rst = f._off.Expr

        (set, rst) --| (f.iop, "O8")

