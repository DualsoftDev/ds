[<AutoOpen>]
module Engine.CodeGenCPU.ConvertButtonLamp

open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
    member s.B1_HWButtonOutput(): CommentedStatement list = [
        for btn in s.HWButtons do
            if btn.OutTag.IsNonNull()  //OutAddress 주소가 있어야 IN-OUT btn 연결
            then
                let set = btn.InTag :?> Tag<bool>
                let out = btn.OutTag :?> Tag<bool>
                yield (set.Expr, s._off.Expr) --| (out, getFuncName())
    ]

    member s.B2_HWLamp(): CommentedStatement list = [
        for lamp in s.HWLamps do
            if lamp.OutTag.IsNonNull() //OutAddress 주소가 ModeLamp 연결
                then
                let sets =
                    let f = lamp.SettingFlows.Head()          
                    match lamp.LampType with
                    | DuAutoLamp      -> f.aop.Expr
                    | DuManualLamp    -> f.mop.Expr
                    | DuDriveLamp     -> f.dop.Expr
                    | DuStopLamp      -> f.sop.Expr
                    | DuEmergencyLamp -> f.eop.Expr
                    | DuTestDriveLamp -> f.top.Expr
                    | DuReadyLamp     -> f.rop.Expr
                    | DuIdleLamp      -> f.iop.Expr

                let out = lamp.OutTag :?> Tag<bool>
                yield (sets, s._off.Expr) --| (out, getFuncName())
    ]

    member s.B3_HWBtnConnetToSW(): CommentedStatement list = [
        for btn in s.HWButtons do
            for flow in  btn.SettingFlows do
                let swTag = 
                    match btn.ButtonType with
                    | DuAutoBTN      -> flow.auto
                    | DuManualBTN    -> flow.manual
                    | DuDriveBTN     -> flow.drive
                    | DuTestBTN      -> flow.test
                    | DuStopBTN      -> flow.stop
                    | DuEmergencyBTN -> flow.emg
                    | DuClearBTN     -> flow.clear
                    | DuHomeBTN      -> flow.home
                    | DuReadyBTN     -> flow.ready
                
                yield (btn.ActionINFunc, s._off.Expr) --| (swTag, getFuncName())
    ]