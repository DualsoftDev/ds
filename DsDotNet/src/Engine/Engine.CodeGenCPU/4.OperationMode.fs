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
                <||> sys.ModeNoExpr <&&> f.ModeManualHWExpr
                //Flow HW A/M 셀렉트 없으면 HMI SW 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeNoHWExpr <&&> !!f.auto.Expr <&&> f.manual.Expr

        let rsts = f.eop.Expr <||> f.rop.Expr <||> f.dop.Expr <||> f.sop.Expr
        (sets, rsts) ==| (f.mop, "O3")
    
    member f.O4_RunOperationMode (): CommentedStatement =
        let sys = f.System
        let sets = 
                sys._auto.Expr <&&> !!sys._manual.Expr <&&> sys._run.Expr
                //시스템 A/M 셀렉트 없으면 Flow HW A or M 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeAutoHWExpr <&&> f.run.Expr
                //Flow HW A/M 셀렉트 없으면 HMI SW 모드를 따라간다.
                <||> sys.ModeNoExpr <&&> f.ModeNoHWExpr <&&> f.auto.Expr <&&> !!f.manual.Expr <&&> sys._run.Expr

        let rsts = f.eop.Expr <||> f.rop.Expr <||> f.dop.Expr <||> f.sop.Expr
        (sets, rsts) ==| (f.mop, "O4")
    
    member f.O5_DryRunOperationMode(): CommentedStatement =
        let sys = f.System
        let hwInputs = if f.manualIns.Any() then f.manualIns.ToOr() else sys._off.Expr
        //시스템 A/M 셀렉트 없으면 Flow HW A or M 모드를 따라간다.
        let systemNoSelect = !!sys._auto.Expr   <&&> !!sys._manual.Expr
        //Flow HW A/M 셀렉트 없으면 HMI SW 모드를 따라간다.
        let flowHWNoSelect = !!f.autoIns.ToOr() <&&> !!f.manualIns.ToOr()
        let flowHMIAuto= f.auto.Expr <&&> !!f.manual.Expr

        let sets = 
                sys._auto.Expr <&&> !!sys._manual.Expr <&&> sys._dryrun.Expr
                <||> systemNoSelect <&&> f.autoIns.ToAnd() <&&> !!f.manualIns.ToOr() <&&> f.dryrun.Expr
                <||> systemNoSelect <&&> flowHWNoSelect     <&&> flowHMIAuto         <&&> f.dryIns.ToAnd()

        let rsts = f.eop.Expr <||> f.mop.Expr <||> f.dop.Expr <||> f.sop.Expr
         
        (sets, rsts) ==| (f.rop, "O5")