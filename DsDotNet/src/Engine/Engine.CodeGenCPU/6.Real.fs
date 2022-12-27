[<AutoOpen>]
module Engine.CodeGenCPU.ConvertReal

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type VertexManager with
    //member realTag.CreateRealEndRung(calls:VertexManager seq) : CommentedStatement =
    //    let sets  =
    //        if calls.Any()
    //        then calls.Select(fun f->f.RR)//.ToTags()  //자식이 있으면 자식완료 릴레이 조건
    //        else [realTag.RR]//.ToTags()               //자식이 없으면 본인시작 릴레이 조건

    //    let statement = realTag.EndTag <==  toAnd (sets.Cast<Tag<bool>>())
    //    statement |> withNoComment

    //member realTag.CreateInitStartRung() : CommentedStatement =
    //    let sets  = [realTag.G;realTag.OG].ToAnd()
    //    let rsts  = [realTag.H].ToAnd()
    //    let relay = realTag.RR

    //    (sets, rsts) ==| (realTag.RR, "")


          //test ahn
    member v.R1_RealInitialStart(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "R1" )

          //test ahn
    member v.R2_RealJobComplete(): CommentedStatement  = 
        (v.PA.Expr, v.OFF.Expr) --| (v.PA, "R2" )

        