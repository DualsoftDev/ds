[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type Flow with

    member f.O1_AutoOperationMode(): CommentedStatement =
        let set = f.ModeAutoHwExpr <&&> f.ModeAutoSwHMIExpr
        let rst = f.rop.Expr

        (set, rst) ==| (f.aop, "O1")

    member f.O2_ManualOperationMode (): CommentedStatement =
        let set = f.ModeManualHwExpr <||> f.ModeManualSwHMIExpr
        let rst = f.rop.Expr

        (set, rst) ==| (f.mop, "O2")

    member f.O3_DriveOperationMode (): CommentedStatement =
        let set = f.aop.Expr <&&> (f.drive.Expr <||> f.BtnDriveExpr)
        let rst = !!f.rop.Expr

        (set, rst) ==| (f.dop, "O3")

    member f.O4_TestRunOperationMode (): CommentedStatement =
        let set = f.aop.Expr <&&>  (f.test.Expr <||> f.BtnTestExpr)
        let rst = !!f.rop.Expr

        (set, rst) ==| (f.top, "O4")

    member f.O5_EmergencyMode(): CommentedStatement =
        let set = f.emg.Expr <||> f.BtnEmgExpr
        let rst = f._off.Expr

        (set, rst) --| (f.eop, "O5")

    member f.O6_StopMode(): CommentedStatement =
        let set = f.stop.Expr <||> f.BtnStopExpr
        let setErrs = f.GetVerticesWithInReal().Select(getVM).ERRs().ToOrElseOff(f.System)
        let rst = f.clear.Expr

        (set <||> setErrs, rst) --| (f.sop, "O6")

    member f.O7_ReadyMode(): CommentedStatement =
        let set = f.ready.Expr <||> f.BtnReadyExpr
        let rst = f.eop.Expr

        (set, rst) ==| (f.rop, "O7")