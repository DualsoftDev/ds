[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexMemoryManager with
    member realTag.CreateRealEndRung(calls:VertexMemoryManager seq) : Statement =
        let sets  =
            if calls.Any()
            then calls.Select(fun f->f.Relay)//.ToTags()  //자식이 있으면 자식완료 릴레이 조건
            else [realTag.Relay]//.ToTags()               //자식이 없으면 본인시작 릴레이 조건

        realTag.EndTag <==  tags2AndExpr (sets.Cast<TagBase<bool>>())

    member realTag.CreateInitStartRung() : Statement =
        let sets  = [realTag.Going;realTag.Origin].ToTags()
        let rsts  = [realTag.Homing].ToTags()
        let relay = realTag.Relay

        realTag.Relay <== FuncExt.GetRelayExpr(sets, rsts, relay)

