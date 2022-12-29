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


    member v.R1_RealInitialStart(): CommentedStatement  = 
        let sets = v.G.Expr <&&> v.OG.Expr  
        let rsts = v.H.Expr

        (sets, rsts) ==| (v.RR, "R1" )

    member v.R2_RealJobComplete(): CommentedStatement  = 
        let real = v.Vertex :?> Real
        let sets = real.CoinRelays.ToAnd()
        let rsts = v.System._off.Expr

        (sets, rsts) --| (v.ET, "R2" )