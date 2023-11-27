[<AutoOpen>]
module Engine.CodeGenCPU.ConvertButtonLamp

open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
    member s.B1_ButtonOutput(): CommentedStatement list = [
        for btn in s.HWButtons do
            if btn.OutTag.IsNonNull()  //OutAddress 주소가 있어야 IN-OUT btn 연결
            then
                let set = btn.InTag :?> Tag<bool>
                let out = btn.OutTag :?> Tag<bool>
                yield (set.Expr, s._off.Expr) --| (out, getFuncName())
    ]

    member s.B2_ModeLamp(): CommentedStatement list = [
        for lamp in s.HWLamps do
            if lamp.OutTag.IsNonNull() //OutAddress 주소가 ModeLamp 연결
                then
                let sets =
                    let f = lamp.SettingFlow
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