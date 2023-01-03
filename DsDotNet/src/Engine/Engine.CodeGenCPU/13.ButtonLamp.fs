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
                let set = 
                    match lamp.LampType with
                    | DuEmergencyLamp   -> lamp.SettingFlow.eop
                    | DuRunModeLamp     -> lamp.SettingFlow.rop
                    | DuDryRunModeLamp  -> lamp.SettingFlow.dop
                    | DuManualModeLamp  -> lamp.SettingFlow.mop
                    | DuStopModeLamp    -> lamp.SettingFlow.sop

                let out = lamp.OutTag :?> PlcTag<bool>
                yield (set.Expr, s._off.Expr) --| (out, "B2" )
        ]