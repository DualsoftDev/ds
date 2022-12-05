[<AutoOpen>]
module Engine.Obsolete.CpuUnit.ConvertReal

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Obsolete.CpuUnit

[<AutoOpen>]
[<Extension>]
type StatementReal =


    [<Extension>] static member CreateRungForRealEnd(realTag:DsMemory, calls:DsMemory seq) =
                    let sets  =
                        if calls.Any()
                        then calls.Select(fun f->f.Relay).ToTags()  //자식이 있으면 자식완료 릴레이 조건
                        else [realTag.Relay].ToTags()               //자식이 없으면 본인시작 릴레이 조건

                    realTag.End <==  FuncExt.GetAnd(sets)

    [<Extension>] static member CreateRungForInitStart(realTag:DsMemory)  =
                    let sets  = [realTag.Going;realTag.Origin].ToTags()
                    let rsts  = [realTag.Homing].ToTags()
                    let relay = realTag.Relay

                    realTag.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)

