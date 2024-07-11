namespace Engine.Parser.FS

open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Engine.Parser

type ParserResult() =
    member val rules = ResizeArray<ParserRuleContext>()
    member val terminals = ResizeArray<ITerminalNode>()
    member val errors = ResizeArray<IErrorNode>()


type AllListener() =
    inherit dsBaseListener()

    member val r = new ParserResult()

    // ParseTreeListener<> method
    override x.VisitTerminal(node: ITerminalNode) = x.r.terminals.Add(node)

    override x.VisitErrorNode(node: IErrorNode) =
        x.r.errors.Add(node)
        ParserError("ERROR while parsing", node) |> raise

    override x.EnterEveryRule(ctx: ParserRuleContext) = x.r.rules.Add(ctx)
    override x.ExitEveryRule(_ctx: ParserRuleContext) = ()
