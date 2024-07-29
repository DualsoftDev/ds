namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuFlow =
    
    let getButtonExpr(flow:Flow, btns:ButtonDef seq) : Expression<bool>  =
        let tags =
            btns
                .Where(fun b -> b.SettingFlows.Contains(flow))
                .Where(fun b ->b.InTag.IsNonNull())
                .Select(fun b ->b.ActionINFunc)
        if tags.any() then tags.ToOrElseOn() else flow.System._off.Expr

    let getConditionsToAndElseOn(flow:Flow, condis:ConditionDef seq) : Expression<bool>  =
        let tags =
            condis
                .Where(fun c -> c.SettingFlows.Contains(flow))
                .Where(fun c ->c.InTag.IsNonNull())
                .Select(fun c -> !@c.ActionINFunc)
        tags.ToAndElseOn()

    type Flow with        
        /// IDLE operation mode
        member f.iop    = getFM(f).GetFlowTag(FlowTag.idle_mode)
        /// AUTO operation mode
        member f.aop    = getFM(f).GetFlowTag(FlowTag.auto_mode)
        /// MANUAL operation mode
        member f.mop    = getFM(f).GetFlowTag(FlowTag.manual_mode)



        member f.d_st    = getFM(f).GetFlowTag(FlowTag.drive_state    )
        member f.t_st    = getFM(f).GetFlowTag(FlowTag.test_state     )
        member f.e_st    = getFM(f).GetFlowTag(FlowTag.error_state)
        member f.emg_st  = getFM(f).GetFlowTag(FlowTag.emergency_state)
        member f.r_st    = getFM(f).GetFlowTag(FlowTag.ready_state    )
        member f.o_st    = getFM(f).GetFlowTag(FlowTag.origin_state)
        member f.g_st    = getFM(f).GetFlowTag(FlowTag.going_state)
        member f.p_st    = getFM(f).GetFlowTag(FlowTag.pause_state )
  
        member f.auto_btn   = getFM(f).GetFlowTag(FlowTag.auto_btn    )
        member f.manual_btn = getFM(f).GetFlowTag(FlowTag.manual_btn  )
        member f.drive_btn  = getFM(f).GetFlowTag(FlowTag.drive_btn   )
        member f.pause_btn  = getFM(f).GetFlowTag(FlowTag.pause_btn   )
        member f.ready_btn  = getFM(f).GetFlowTag(FlowTag.ready_btn   )
        member f.clear_btn  = getFM(f).GetFlowTag(FlowTag.clear_btn   )
        member f.emg_btn    = getFM(f).GetFlowTag(FlowTag.emg_btn     )
        member f.test_btn   = getFM(f).GetFlowTag(FlowTag.test_btn    )
        member f.home_btn   = getFM(f).GetFlowTag(FlowTag.home_btn    )
        
        member f.auto_lamp   = getFM(f).GetFlowTag(FlowTag.auto_lamp    )
        member f.manual_lamp = getFM(f).GetFlowTag(FlowTag.manual_lamp  )
        member f.drive_lamp  = getFM(f).GetFlowTag(FlowTag.drive_lamp   )
        member f.pause_lamp  = getFM(f).GetFlowTag(FlowTag.pause_lamp    )
        member f.ready_lamp  = getFM(f).GetFlowTag(FlowTag.ready_lamp   )
        member f.clear_lamp  = getFM(f).GetFlowTag(FlowTag.clear_lamp   )
        member f.emg_lamp    = getFM(f).GetFlowTag(FlowTag.emg_lamp     )
        member f.test_lamp   = getFM(f).GetFlowTag(FlowTag.test_lamp    )
        member f.home_lamp   = getFM(f).GetFlowTag(FlowTag.home_lamp    )

        member f.stopError  = getFM(f).GetFlowTag(FlowTag.flowStopError    )
        member f.readyCondition= getFM(f).GetFlowTag(FlowTag.flowReadyCondition )
        member f.driveCondition= getFM(f).GetFlowTag(FlowTag.flowDriveCondition )
        member f.F = f |> getFM
        member f._on     = f.System._on
        member f._off    = f.System._off
        member f._sim    = f.System._sim
        //select 버튼은 없을경우 항상 _on
        member f.HwAutoSelects =  f.System.AutoHWButtons.Where(fun b->b.SettingFlows.Contains(f))
        member f.HwManuSelects =  f.System.ManualHWButtons.Where(fun b->b.SettingFlows.Contains(f))
        member f.HwAutoExpr = getButtonExpr(f, f.System.AutoHWButtons  )
        member f.HwManuExpr = getButtonExpr(f, f.System.ManualHWButtons)

        //push 버튼은 없을경우 항상 _off
        member private f.HWBtnDriveExpr = getButtonExpr(f, f.System.DriveHWButtons    ) 
        member private f.HWBtnPauseExpr = getButtonExpr(f, f.System.PauseHWButtons     )
        member private f.HWBtnEmgExpr   = getButtonExpr(f, f.System.EmergencyHWButtons)
        member private f.HWBtnTestExpr  = getButtonExpr(f, f.System.TestHWButtons     )
        member private f.HWBtnReadyExpr = getButtonExpr(f, f.System.ReadyHWButtons    ) 
        member private f.HWBtnClearExpr = getButtonExpr(f, f.System.ClearHWButtons    )
        member private f.HWBtnHomeExpr  = getButtonExpr(f, f.System.HomeHWButtons     )  

        member f.HWReadyConditionsToAndElseOn = getConditionsToAndElseOn(f, f.System.HWConditions.Where(fun f->f.ConditionType = DuReadyState))
        member f.HWDriveConditionsToAndElseOn = getConditionsToAndElseOn(f, f.System.HWConditions.Where(fun f->f.ConditionType = DuDriveState))
           

        member f.AutoSelectExpr   =  f.auto_btn.Expr   <||> f.System._auto_btn.Expr     <||> f.HwAutoExpr
        member f.ManuSelectExpr   =  f.manual_btn.Expr <||> f.System._manual_btn.Expr   <||> f.HwManuExpr

        member f.HomeExpr   =  f.home_btn.Expr   <||> f.System._home_btn.Expr     <||> f.HWBtnHomeExpr
        member f.TestExpr   =  f.test_btn.Expr   <||> f.System._test_btn.Expr     <||> f.HWBtnTestExpr
        member f.EmgExpr    =  f.emg_btn.Expr    <||> f.System._emg_btn.Expr      <||> f.HWBtnEmgExpr
        member f.DriveExpr  =  f.drive_btn.Expr  <||> f.System._drive_btn.Expr    <||> f.HWBtnDriveExpr
        member f.PauseExpr  =  f.pause_btn.Expr  <||> f.System._pause_btn.Expr    <||> f.HWBtnPauseExpr
        member f.ReadyExpr  =  f.ready_btn.Expr  <||> f.System._ready_btn.Expr    <||> f.HWBtnReadyExpr
        member f.ClearExpr  =  f.clear_btn.Expr  <||> f.System._clear_btn.Expr    <||> f.HWBtnClearExpr
        member f.AutoExpr   =  f.AutoSelectExpr <&&> !@f.ManuSelectExpr
        member f.ManuExpr   =  !@f.AutoSelectExpr <&&> f.ManuSelectExpr
               
        member f.GetReadAbleTags() =
            FlowTag.GetValues(typeof<FlowTag>)
                .Cast<FlowTag>()
                .Select(getFM(f).GetFlowTag)

        member f.GetWriteAbleTags() =
            let writeAble =
                [   FlowTag.auto_btn
                    FlowTag.manual_btn
                    FlowTag.drive_btn
                    FlowTag.pause_btn
                    FlowTag.ready_btn
                    FlowTag.clear_btn
                    FlowTag.emg_btn
                    FlowTag.test_btn
                ]
            writeAble |> map (getFM(f).GetFlowTag)
