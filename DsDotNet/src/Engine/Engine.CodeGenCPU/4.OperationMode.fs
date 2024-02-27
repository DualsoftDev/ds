[<AutoOpen>]
module Engine.CodeGenCPU.ConvertOperationMode

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with

    member f.O1_ReadyOperationState() =
        let set = f.ready_btn.Expr <||> f.HWBtnReadyExpr 
        let rst = (f.eop.Expr <||> f.emg.Expr) <||> (f.stopConditionErr.Expr <&&> !!f._sim.Expr)

        (set, rst) ==| (f.rop, getFuncName())

    member f.O2_AutoOperationState() =
        let set = f.AutoExpr 
        let rst = !!f.rop.Expr
        
        (set, rst) --| (f.aop, getFuncName())
            

    member f.O3_ManualOperationState () =
        let set = f.ManuExpr
        let rst = !!f.rop.Expr 
        
        (set, rst) --| (f.mop, getFuncName())


    member f.O4_EmergencyOperationState() =
        let set = f.emg_btn.Expr <||> f.HWBtnEmgExpr
        let rst = f.clear_btn.Expr

        (set, rst) ==| (f.emg, getFuncName())

    member f.O5_StopOperationState() =
        let setDeviceError = (f.Graph.Vertices.OfType<Real>().Select(getVM) 
                                |> Seq.collect(fun r-> [|r.ErrTRX|])).ToOrElseOff()
        let setConditionError = f.stopConditionErr.Expr <&&> !!f._sim.Expr
        let set =  setDeviceError<||> setConditionError
        let rst = f.clear_btn.Expr
           
        (set, rst) ==| (f.eop, getFuncName())

    member f.O6_DriveOperationMode () =
        let set = f.drive_btn.Expr <||> f.HWBtnDriveExpr    
        let rst = !!f.aop.Expr <||>  f.top.Expr

        (set, rst) ==| (f.dop, getFuncName())

    member f.O7_TestOperationMode () =
        let set = f.test_btn.Expr <||> f.HWBtnTestExpr
        let rst = !!f.aop.Expr <||> f.dop.Expr

        (set, rst) ==| (f.top, getFuncName())

    member f.O8_IdleOperationMode() =
        let set = !!(f.dop.Expr <||> f.top.Expr)
        let rst = f._off.Expr

        (set, rst) --| (f.iop, getFuncName())

    member f.O9_originOperationMode() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.OG).ToAndElseOn()
        let rst = f._off.Expr

        (set, rst) --| (f.oop, getFuncName())   

    member f.O10_homingOperationMode() =
        let set = f.home_btn.Expr <||> f.HWBtnHomeExpr
        let rst = f._off.Expr

        (set, rst) --| (f.hop, getFuncName())


    member f.O11_goingOperationMode() =
        let set = f.Graph.Vertices.OfType<Real>().Select(getVM)
                   .Select(fun r-> r.G).ToOrElseOn()  <&&> f.dop.Expr
        let rst = f._off.Expr

        (set, rst) --| (f.gop, getFuncName())

