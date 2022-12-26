[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexMemoryManager with
    member realTag.CreateRealEndRung(calls:VertexMemoryManager seq) : CommentedStatement =
        let sets  =
            if calls.Any()
            then calls.Select(fun f->f.RelayRealInitStart)//.ToTags()  //자식이 있으면 자식완료 릴레이 조건
            else [realTag.RelayRealInitStart]//.ToTags()               //자식이 없으면 본인시작 릴레이 조건

        let statement = realTag.EndTag <==  tags2AndExpr (sets.Cast<Tag<bool>>())
        statement |> withNoComment

    member realTag.CreateInitStartRung() : CommentedStatement =
        let sets  = [realTag.Going;realTag.Origin].ToAnd()
        let rsts  = [realTag.Homing].ToAnd()
        let relay = realTag.RelayRealInitStart

        let statement = realTag.RelayRealInitStart.GetRelay(sets, rsts)
        statement |> withNoComment
