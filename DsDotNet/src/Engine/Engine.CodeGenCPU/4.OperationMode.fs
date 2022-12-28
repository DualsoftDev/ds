[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type Flow with
    
    member f.O1_EmergencyOperationMode(): CommentedStatement =
        //ahn : emg IO 입력 추가필요
        let sys = f.System
        let sets = sys._emg.Expr <||> f.emg.Expr
        let rsts = sys._off.Expr
         
        (sets, rsts) --| (f.eop, "O1")

    member f.O2_StopOperationMode(): CommentedStatement =
        //ahn :  IO 입력 추가필요
        let sets = f.System._stop.Expr <||> f.stop.Expr
        let rsts = f.System._off.Expr
         
        (sets, rsts) ==| (f.sop, "O2")
    
    member f.O3_ManualOperationMode (): CommentedStatement =
        //ahn :  IO 입력 추가필요
        let sets = f.System._manual.Expr <||> f.manual.Expr
        let rsts = f.System._off.Expr
         
        (sets, rsts) ==| (f.mop, "O3")
    
    member f.O4_RunOperationMode (): CommentedStatement =
        //ahn :  IO 입력 추가필요
        let sets = f.System._run.Expr <||> f.run.Expr
        let rsts = f.System._off.Expr
         
        (sets, rsts) ==| (f.rop, "O4")
    
    member f.O5_DryRunOperationMode(): CommentedStatement =
        //ahn :  IO 입력 추가필요
        let sets = f.System._dryrun.Expr <||> f.dryrun.Expr
        let rsts = f.System._off.Expr
         
        (sets, rsts) ==| (f.dop, "O5")