[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type Flow with
    
    member f.O1_AutoOperationMode(): CommentedStatement =
        let sets = f.ModeAutoHwExpr <&&> f.ModeAutoSwHMIExpr
        let rsts = f.rop.Expr
         
        (sets, rsts) ==| (f.aop, "O1")
    
    member f.O2_ManualOperationMode (): CommentedStatement =
        let sets = f.ModeManualHwExpr <||> f.ModeManualSwHMIExpr
        let rsts = f.rop.Expr
         
        (sets, rsts) ==| (f.mop, "O2")
  
    member f.O3_DriveOperationMode (): CommentedStatement =
        let sets = f.aop.Expr <&&> (f.drive.Expr <||> f.BtnDriveExpr)
        let rsts = !!f.rop.Expr

        (sets, rsts) ==| (f.dop, "O3")
    
    member f.O4_TestRunOperationMode (): CommentedStatement =
        let sets = f.aop.Expr <&&>  (f.test.Expr <||> f.BtnTestExpr)
        let rsts = !!f.rop.Expr

        (sets, rsts) ==| (f.top, "O4")
    
    member f.O5_EmergencyMode(): CommentedStatement =
        let sets = f.emg.Expr <||> f.BtnEmgExpr
        let rsts = f.System._off.Expr

        (sets, rsts) --| (f.eop, "O5")

    member f.O6_StopMode(): CommentedStatement =
        let sets = f.stop.Expr <||> f.BtnStopExpr
        let setErrs = f.GetVerticesWithInReal().Select(getVM).ERRs().ToOrElseOff(f.System)
        let rsts = f.clear.Expr

        (sets <||> setErrs, rsts) --| (f.sop, "O6")

    member f.O7_ReadyMode(): CommentedStatement =
        let sets = f.ready.Expr <||> f.BtnReadyExpr
        let rsts = !!f.rop.Expr

        (sets, rsts) ==| (f.rop, "O7")