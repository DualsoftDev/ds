[<AutoOpen>]
module Engine.CodeGenCPU.ConvertButtonLamp

open System.Linq
open Engine.Core
open Engine.CodeGenCPU



type DsSystem with
    member s.B1_ButtonOutput(): CommentedStatement list=
        [
            for btn in s.SystemButtons do
                let set = btn.InTag :?> PlcTag<bool>
                let out = btn.OutTag :?> PlcTag<bool>
                yield (set.Expr, s._off.Expr) --| (out, "B1" )
        ]

    member s.B2_ModeLamp(): CommentedStatement list=
        [
            for lamp in s.SystemLamps do
                let sets = 
                    match lamp.LampType with
                    | DuAutoModeLamp      -> lamp.SettingFlow.aop.Expr
                    | DuManualModeLamp    -> lamp.SettingFlow.mop.Expr
                    | DuDriveModeLamp     -> lamp.SettingFlow.dop.Expr
                    | DuStopModeLamp      -> lamp.SettingFlow.sop.Expr
                    | DuEmergencyModeLamp -> lamp.SettingFlow.eop.Expr
                    | DuTestModeLamp      -> lamp.SettingFlow.top.Expr
                    | DuReadyModeLamp     -> lamp.SettingFlow.rop.Expr

                let out = lamp.OutTag :?> PlcTag<bool>
                yield (sets, s._off.Expr) --| (out, "B2" )
        ]