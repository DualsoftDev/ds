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
                let sets = btn.ActionINFunc
                let out = btn.OutTag :?> Tag<bool>
                yield (sets, s._off.Expr) --| (out, getFuncName())
    ]

    member s.B2_HWLamp(): CommentedStatement list = [
        for lamp in s.HWLamps do
            let modeBit =
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

            let sets = if lamp.InTag.IsNull()
                       then modeBit 
                       else modeBit <||>  lamp.ActionINFunc

            let out = lamp.OutTag :?> Tag<bool>
            yield (sets, s._off.Expr) --| (out, getFuncName())
    ]

    member s.B3_HWBtnConnetToSW(): CommentedStatement list =  [
        for cond in s.HWConditions do
            if cond.OutTag.IsNonNull()  //OutAddress 주소가 있어야 IN-OUT cond Check Lamp 연결
            then
                let sets = cond.ActionINFunc
                let out = cond.OutTag :?> Tag<bool>
                yield (sets, s._off.Expr) --| (out, getFuncName())
    ]