namespace Engine.Parser.FS

open System.Collections.Generic
open System.Runtime.CompilerServices
open Antlr4.Runtime
open Antlr4.Runtime.Misc

[<AutoOpen>]
module ParserUtil =
    let getRange (ctx:ParserRuleContext) =
        let s = ctx.Start.StartIndex
        let e = ctx.Stop.StopIndex
        s, e

    let getOriginalText (ctx:ParserRuleContext) =
        // https://stackoverflow.com/questions/16343288/how-do-i-get-the-original-text-that-an-antlr4-rule-matched
        ctx.Start.InputStream.GetText(getRange ctx |> Interval)

    type RangeReplace = {
        Start:int
        End:int
        ReplaceText: string
    } with
        static member Create(ctx:ParserRuleContext, replace: string, ?baseOffsetContext:ParserRuleContext) =
            let baseOffset =
                match baseOffsetContext with
                | Some b -> b.Start.StartIndex
                | None -> 0
            { Start = ctx.Start.StartIndex - baseOffset
              End = ctx.Stop.StopIndex - baseOffset
              ReplaceText = replace }


    let getReplacedText (ruleContextToTextify:ParserRuleContext) (replaces:RangeReplace seq) =
        let text = getOriginalText ruleContextToTextify
        let offset = ruleContextToTextify.Start.StartIndex
        let hash = HashSet<RangeReplace>()
        let rec helper (i:int) =
            [
                if i < text.Length then
                    let range = replaces |> Seq.tryFind (fun r -> r.Start - offset - 1 <= i && i <= r.End - offset)       // -1 for bug???
                    match range with
                    | Some r ->
                        if not (hash.Contains(r)) then
                            hash.Add(r) |> ignore
                            yield! r.ReplaceText
                    | None ->
                        yield text[i]
                    yield! helper (i+1)
            ]
        helper 0 |> Array.ofSeq |> System.String


[<Extension>]
type ParserExt =
    [<Extension>] static member GetOriginalText(ctx:ParserRuleContext) = getOriginalText ctx
    [<Extension>] static member GetReplacedText(ctx:ParserRuleContext, replaces:RangeReplace seq) = getReplacedText ctx replaces
