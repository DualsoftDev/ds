[<AutoOpen>]
module Engine.CodeGenCPU.ConvertButtonLamp

open Engine.Core
open Engine.CodeGenCPU

type DsSystem with
    member s.B1_ButtonOutput(): CommentedStatement list = [
        for btn in s.SystemButtons do
            let set = btn.InTag :?> BridgeTag<bool>
            let out = btn.OutTag :?> BridgeTag<bool>
            yield (set.Expr, s._off.Expr) --| (out, "B1" )
    ]

    member s.B2_ModeLamp(): CommentedStatement list = [
        for lamp in s.SystemLamps do
            let sets =
                let f = lamp.SettingFlow
                match lamp.LampType with
                | DuAutoModeLamp      -> f.aop.Expr
                | DuManualModeLamp    -> f.mop.Expr
                | DuDriveModeLamp     -> f.dop.Expr
                | DuStopModeLamp      -> f.sop.Expr
                | DuEmergencyModeLamp -> f.eop.Expr
                | DuTestModeLamp      -> f.top.Expr
                | DuReadyModeLamp     -> f.rop.Expr

            let out = lamp.OutTag :?> BridgeTag<bool>
            yield (sets, s._off.Expr) --| (out, "B2" )
    ]