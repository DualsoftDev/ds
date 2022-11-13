namespace Engine.Parser.FS

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open System.Runtime.CompilerServices

open Antlr4.Runtime
open Antlr4.Runtime.Misc

open Engine.Core
open Engine.Common.FS
open type Engine.Parser.dsParser

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

        let mutable i = 0
        let folder (z:string) (x:char) =
            let range = replaces |> Seq.tryFind (fun r -> r.Start - offset - 1 <= i && i <= r.End - offset)       // -1 for bug???
            i <- i + 1
            match range with
            | Some r ->
                if hash.Contains(r) then
                    z
                else
                    hash.Add(r) |> ignore
                    z + r.ReplaceText
            | None ->
                z + (x |> string)
        let replaced = text.ToList().FoldLeft(folder, "")
        replaced

    [<DebuggerDisplay("{FullName}({ContextType.Name})")>]
    type ContextInformation = {
        Systems: string list
        Flow: string option
        Parenting: string option
        Names: string list
        ContextType:System.Type
    } with
        static member Create(parserRuleContext, systems:string list, flow, parenting, names) =
            if systems.Any(fun s -> s.Contains "localhost") then
                noop()
            {   ContextType = parserRuleContext.GetType();
                Systems = systems; Flow = flow; Parenting = parenting; Names = names }
        static member CreateFullNameComparer() = {
            new IEqualityComparer<ContextInformation> with
                member _.Equals(x:ContextInformation, y:ContextInformation) = x.FullName = y.FullName
                member _.GetHashCode(x:ContextInformation) = x.FullName.GetHashCode()
        }

        member x.Tuples = x.Systems, x.Flow, x.Parenting, x.Names
        member x.NameComponents = [
            yield! x.Systems
            if x.Flow.IsSome then yield x.Flow.Value
            if x.Parenting.IsSome then yield x.Parenting.Value
            if x.ContextType = typedefof<SystemContext>
                || x.ContextType = typedefof<FlowContext>
                || x.ContextType = typedefof<ParentingContext>
                then
                ()
            else
                yield! x.Names
        ]
        member x.FullName = x.NameComponents.ToArray().Combine()

    let getParentWrapper (ci:ContextInformation) (flow:Flow option) (parenting:Real option) =
        match ci.Parenting with
        | Some prnt -> Real parenting.Value
        | None -> Flow flow.Value

[<Extension>]
type ParserExt =
    [<Extension>] static member GetOriginalText(ctx:ParserRuleContext) = getOriginalText ctx
    [<Extension>] static member GetReplacedText(ctx:ParserRuleContext, replaces:RangeReplace seq) = getReplacedText ctx replaces
