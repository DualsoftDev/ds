[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with

    member f.O1_ReadyOperationState(): CommentedStatement =
        let set = f.ready.Expr <||> f.BtnReadyExpr
        let rst = f.eop.Expr <||> f.sop.Expr

        (set, rst) ==| (f.rop, getFuncName())

    member f.O2_AutoOperationState(): CommentedStatement =
        let set = f.ModeAutoHwExpr// <&&> f.ModeAutoSwHMIExpr  //test ahn lightPLC 모드 준비중
        let rst = !!f.rop.Expr <||> f.ModeManualHwExpr

        (set, rst) ==| (f.aop, getFuncName())

    member f.O3_ManualOperationState (): CommentedStatement =
        let set = f.ModeManualHwExpr// <||> f.ModeManualSwHMIExpr
        let rst = !!f.rop.Expr <||> f.ModeAutoHwExpr

        (set, rst) ==| (f.mop, getFuncName())


    member f.O4_EmergencyOperationState(): CommentedStatement =
        let set = f.emg.Expr <||> f.BtnEmgExpr
        let rst = f._off.Expr

        (set, rst) --| (f.eop, getFuncName())

    member f.O5_StopOperationState(): CommentedStatement =
        let set = (f.stop.Expr <||> f.BtnStopExpr <||> f.sop.Expr) <&&> !!f.BtnClearExpr
        let setErrs = f.Graph.Vertices.OfType<Real>().Select(getVM) 
                        |> Seq.collect(fun r-> [|r.E1; r.E2|])
        let rst = f.clear.Expr //test ahn lightPLC 모드 준비중

        (set <||> setErrs.ToOrElseOff(f.System), rst) ==| (f.sop, getFuncName())

    member f.O6_DriveOperationMode (): CommentedStatement =
        let set = f.drive.Expr <||> f.BtnDriveExpr
        let rst = !!f.aop.Expr <||>  f.top.Expr

        (set, rst) ==| (f.dop, getFuncName())

    member f.O7_TestOperationMode (): CommentedStatement =
        let set = f.test.Expr <||> f.BtnTestExpr
        let rst = !!f.aop.Expr <||> f.dop.Expr

        (set, rst) ==| (f.top, getFuncName())

    member f.O8_IdleOperationMode(): CommentedStatement =
        let set = !!(f.dop.Expr <||> f.top.Expr)
        let rst = f._off.Expr

        (set, rst) --| (f.iop, getFuncName())

