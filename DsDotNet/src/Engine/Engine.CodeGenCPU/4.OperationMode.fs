[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU


type Flow with
    
    member f.O1_EmergencyOperationMode(): CommentedStatement =
        let sets = f.EmgExpr
        let rsts = f.System._off.Expr
         
        (sets, rsts) --| (f.eop, "O1")

    member f.O2_StopOperationMode(): CommentedStatement =
        let sets = f.StopExpr
        let rsts = f.eop.Expr <||> f.clear.Expr <||> f.System._clear.Expr
         
        (sets, rsts) ==| (f.sop, "O2")
    
    member f.O3_ManualOperationMode (): CommentedStatement =
        let sys = f.System
        let sets = 
                !!sys._auto.Expr <&&> sys._manual.Expr
                //시스템 A/M 셀렉트 없으면 Flow HW A or M 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeManualHwExpr
                //Flow HW A/M 셀렉트 없으면 HMI SW 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeNoHWExpr <&&> f.ModeManualSwHMIExpr

        let rsts = f.eop.Expr <||> f.rop.Expr <||> f.dop.Expr <||> f.sop.Expr
        (sets, rsts) ==| (f.mop, "O3")
    
    member f.O4_RunOperationMode (): CommentedStatement =
        let sys = f.System
        let sets = 
                sys._auto.Expr <&&> !!sys._manual.Expr <&&> f.DriveExpr
                //시스템 A/M 셀렉트 없으면 Flow HW A or M 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeAutoHwExpr <&&> f.DriveExpr
                //Flow HW A/M 셀렉트 없으면 HMI SW 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeNoHWExpr <&&> f.ModeAutoSwHMIExpr <&&> f.DriveExpr

        let rsts = f.eop.Expr <||> f.rop.Expr <||> f.dop.Expr <||> f.sop.Expr
        (sets, rsts) ==| (f.mop, "O4")
    
    member f.O5_DryRunOperationMode(): CommentedStatement =
        let sys = f.System
        let sets = 
                sys._auto.Expr <&&> !!sys._manual.Expr <&&> f.TestExpr
                //시스템 A/M 셀렉트 없으면 Flow HW A or M 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeAutoHwExpr <&&> f.TestExpr
                //Flow HW A/M 셀렉트 없으면 HMI SW 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeNoHWExpr <&&> f.ModeAutoSwHMIExpr <&&> f.TestExpr

        let rsts = f.eop.Expr <||> f.rop.Expr <||> f.dop.Expr <||> f.sop.Expr
        (sets, rsts) ==| (f.mop, "O5")