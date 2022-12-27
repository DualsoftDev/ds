[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexManager with
    member realTag.CreateRealEndRung(calls:VertexManager seq) : CommentedStatement =
        let sets  =
            if calls.Any()
            then calls.Select(fun f->f.RelayRealInitStart)//.ToTags()  //자식이 있으면 자식완료 릴레이 조건
            else [realTag.RelayRealInitStart]//.ToTags()               //자식이 없으면 본인시작 릴레이 조건

        let statement = realTag.EndTag <==  toAnd (sets.Cast<Tag<bool>>())
        statement |> withNoComment

    member realTag.CreateInitStartRung() : CommentedStatement =
        let sets  = [realTag.Going;realTag.Origin].ToAnd()
        let rsts  = [realTag.Homing].ToAnd()
        let relay = realTag.RelayRealInitStart

        (sets, rsts) ==| (realTag.RelayRealInitStart, "")
