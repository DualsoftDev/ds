[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type Flow with
    
    member f.O1_EmergencyOperationMode(): CommentedStatement =
        let sys = f.System
        //test ahn : plctag b접점 옵션 반영필요
        let hwInputs = if f.emgIns.Any() then f.emgIns.ToOr() else sys._off.Expr
        let sets = sys._emg.Expr <||> f.emg.Expr <||> hwInputs
        let rsts = sys._off.Expr
         
        (sets, rsts) --| (f.eop, "O1")

    member f.O2_StopOperationMode(): CommentedStatement =
        let sys = f.System
        let hwInputs = if f.stopIns.Any() then f.stopIns.ToOr() else sys._off.Expr
        let sets = sys._stop.Expr <||> f.stop.Expr <||> hwInputs
        let rsts = f.eop.Expr <||> f.clear.Expr <||> sys._clear.Expr
         
        (sets, rsts) ==| (f.sop, "O2")
    
    
    member f.O3_ManualOperationMode (): CommentedStatement =
        let sys = f.System
        let hwInputs = if f.manualIns.Any() then f.manualIns.ToOr() else sys._off.Expr
        //시스템 A/M 셀렉트 없으면 Flow A/M 모드를 따라간다.
        let localMode    = !!sys._auto.Expr <&&> !!sys._manual.Expr
        let systemManual = !!sys._auto.Expr <&&> sys._manual.Expr
        let flowManual   = !!f.auto.Expr <&&> f.manual.Expr

        let sets = systemManual <||> (localMode <&&> flowManual)
        let rsts = f.eop.Expr <||> f.rop.Expr <||> f.dop.Expr <||> f.sop.Expr
         
        (sets, rsts) ==| (f.mop, "O3")
    
    member f.O4_RunOperationMode (): CommentedStatement =
        let sys = f.System
        let hwInputs = if f.manualIns.Any() then f.manualIns.ToOr() else sys._off.Expr
        let sets = sys._stop.Expr <||> f.stop.Expr <||> hwInputs
        let rsts = f.eop.Expr <||> f.mop.Expr <||> f.dop.Expr <||> f.sop.Expr
         
        (sets, rsts) ==| (f.mop, "O4")
    
    member f.O5_DryRunOperationMode(): CommentedStatement =
        //ahn :  IO 입력 추가필요
        let sets = f.System._dryrun.Expr <||> f.dryrun.Expr
        let rsts = f.System._off.Expr
         
        (sets, rsts) ==| (f.dop, "O5")