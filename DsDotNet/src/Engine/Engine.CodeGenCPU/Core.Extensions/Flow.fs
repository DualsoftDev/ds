namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open System
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuFlow =
    
    let getButtonExpr(flow:Flow, btns:ButtonDef seq) : Expression<bool>  =
        let tags = btns.Where(fun b -> b.SettingFlows.Contains(flow))
                       .Where(fun b ->b.InTag.IsNonNull())
                       .Select(fun b ->b.ActionINFunc)
        if tags.any() then tags.ToOrElseOn() else flow.System._off.Expr

    let getConditionsToAndElseOn(flow:Flow, condis:ConditionDef seq) : Expression<bool>  =
        let tags = condis
                    .Where(fun c -> c.SettingFlows.Contains(flow))
                    .Where(fun c ->c.InTag.IsNonNull())
                    .Select(fun c -> !!c.ActionINFunc)
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
        member f.emg_st    = getFM(f).GetFlowTag(FlowTag.emergency_state)
        member f.r_st    = getFM(f).GetFlowTag(FlowTag.ready_state    )
        member f.o_st    = getFM(f).GetFlowTag(FlowTag.origin_state)
        member f.g_st    = getFM(f).GetFlowTag(FlowTag.going_state)
  
        member f.auto_btn   = getFM(f).GetFlowTag(FlowTag.auto_btn    )
        member f.manual_btn = getFM(f).GetFlowTag(FlowTag.manual_btn  )
        member f.drive_btn  = getFM(f).GetFlowTag(FlowTag.drive_btn   )
        member f.pause_btn  = getFM(f).GetFlowTag(FlowTag.pause_btn   )
        member f.ready_btn  = getFM(f).GetFlowTag(FlowTag.ready_btn   )
        member f.clear_btn  = getFM(f).GetFlowTag(FlowTag.clear_btn   )
        member f.emg_btn    = getFM(f).GetFlowTag(FlowTag.emg_btn     )
        member f.test_btn   = getFM(f).GetFlowTag(FlowTag.test_btn    )

        member f.auto_lamp   = getFM(f).GetFlowTag(FlowTag.auto_lamp    )
        member f.manual_lamp = getFM(f).GetFlowTag(FlowTag.manual_lamp  )
        member f.drive_lamp  = getFM(f).GetFlowTag(FlowTag.drive_lamp   )
        member f.pause_lamp  = getFM(f).GetFlowTag(FlowTag.pause_lamp    )
        member f.ready_lamp  = getFM(f).GetFlowTag(FlowTag.ready_lamp   )
        member f.clear_lamp  = getFM(f).GetFlowTag(FlowTag.clear_lamp   )
        member f.emg_lamp    = getFM(f).GetFlowTag(FlowTag.emg_lamp     )
        member f.test_lamp   = getFM(f).GetFlowTag(FlowTag.test_lamp    )

        member f.stopError  = getFM(f).GetFlowTag(FlowTag.flowStopError    )
        member f.readyCondition= getFM(f).GetFlowTag(FlowTag.flowReadyCondition )
        member f.driveCondition= getFM(f).GetFlowTag(FlowTag.flowDriveCondition )
        member f.pause    = getFM(f).GetFlowTag(FlowTag.flowPause    )
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
        member f.HWBtnDriveExpr = getButtonExpr(f, f.System.DriveHWButtons    ) 
        member f.HWBtnPauseExpr = getButtonExpr(f, f.System.PauseHWButtons     )
        member f.HWBtnEmgExpr   = getButtonExpr(f, f.System.EmergencyHWButtons)
        member f.HWBtnTestExpr  = getButtonExpr(f, f.System.TestHWButtons     )
        member f.HWBtnReadyExpr = getButtonExpr(f, f.System.ReadyHWButtons    ) 
        member f.HWBtnClearExpr = getButtonExpr(f, f.System.ClearHWButtons    )
        member f.HWBtnHomeExpr  = getButtonExpr(f, f.System.HomeHWButtons     )

        member f.HWReadyConditionsToAndElseOn = getConditionsToAndElseOn(f, f.System.HWConditions.Where(fun f->f.ConditionType = DuReadyState))
        member f.HWDriveConditionsToAndElseOn = getConditionsToAndElseOn(f, f.System.HWConditions.Where(fun f->f.ConditionType = DuDriveState))

        member f.AutoExpr   =  
                let hmiAuto = f.auto_btn.Expr <&&> !!f.manual_btn.Expr
                let hwAuto  = f.HwAutoExpr <&&> !!f.HwManuExpr
                if f.HwAutoSelects.any() //반드시 a/m 쌍으로 존재함  checkErrHWItem 체크중
                then (hwAuto <||> f._sim.Expr) <&&> hmiAuto //HW, HMI Select and 처리
                else hmiAuto

        member f.ManuExpr   =  
                let hmiManu = !!f.auto_btn.Expr <&&> f.manual_btn.Expr
                let hwManu  = !!f.HwAutoExpr <&&> f.HwManuExpr
                if f.HwManuSelects.any() 
                then (hwManu <||> f._sim.Expr) <||> hmiManu //HW, HMI Select or 처리
                else hmiManu

        member f.GetReadAbleTags() =
            FlowTag.GetValues(typeof<FlowTag>).Cast<FlowTag>()
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
