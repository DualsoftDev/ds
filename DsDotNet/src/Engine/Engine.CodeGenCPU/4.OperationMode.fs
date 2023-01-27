[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type Flow with

    member f.O1_ReadyMode(): CommentedStatement =
        let set = f.ready.Expr <||> f.BtnReadyExpr
        let rst = !!f.scr.Expr <||> f.eop.Expr <||> f.sop.Expr

        (set, rst) ==| (f.rop, "O1")

    member f.O2_AutoOperationMode(): CommentedStatement =
        let set = f.ModeAutoHwExpr <&&> f.ModeAutoSwHMIExpr
        let rst = !!f.rop.Expr <||> f.mop.Expr

        (set, rst) ==| (f.aop, "O2")

    member f.O3_ManualOperationMode (): CommentedStatement =
        let set = f.ModeManualHwExpr <||> f.ModeManualSwHMIExpr
        let rst = !!f.rop.Expr <||> f.aop.Expr

        (set, rst) ==| (f.mop, "O3")

    member f.O4_DriveOperationMode (): CommentedStatement =
        let set = f.drive.Expr <||> f.BtnDriveExpr
        let rst = !!f.aop.Expr <||> !!f.scd.Expr  <||> f.top.Expr

        (set, rst) ==| (f.dop, "O4")

    member f.O5_TestRunOperationMode (): CommentedStatement =
        let set = f.test.Expr <||> f.BtnTestExpr
        let rst = !!f.aop.Expr <||> !!f.scd.Expr  <||> f.dop.Expr

        (set, rst) ==| (f.top, "O5")

    member f.O6_EmergencyMode(): CommentedStatement =
        let set = f.emg.Expr <||> f.BtnEmgExpr
        let rst = f._off.Expr

        (set, rst) --| (f.eop, "O6")

    member f.O7_StopMode(): CommentedStatement =
        let set = f.stop.Expr <||> f.BtnStopExpr
        let setErrs = f.GetVerticesWithInReal().Select(getVM).ERRs().ToOrElseOff(f.System)
        let rst = f.clear.Expr

        (set <||> setErrs, rst) --| (f.sop, "O7")

