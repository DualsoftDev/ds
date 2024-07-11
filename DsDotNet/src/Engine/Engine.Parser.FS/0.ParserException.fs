namespace Engine.Parser.FS

open System
open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Dual.Common.Core.FS

type ParserError(message: string) =
    inherit Exception(message)

    static let CreatePositionInfo (ctx: obj) = // RuleContext or IErrorNode
        let getPosition (ctx: obj) =
            let fromToken (token: IToken) = $"[line:{token.Line}, column:{token.Column}]"

            let fromErrorNode (errNode: IErrorNode) =
                match errNode with
                | :? ErrorNodeImpl as impl -> fromToken (impl.Symbol)
                | _ -> failwithlog "ERROR"

            match ctx with
            | :? ParserRuleContext as prctx ->
                match prctx.Start with
                | :? CommonToken as start -> fromToken (start)
                | _ -> failwithlog "ERROR"
            | :? IErrorNode as errNode -> fromErrorNode (errNode)
            | _ -> failwithlog "ERROR"

        let getAmbient (ctx: obj) =
            match ctx with
            | :? IParseTree as pt -> pt.GetText()
            | _ -> failwithlog "ERROR"

        let posi = getPosition (ctx)
        let ambient = getAmbient (ctx)
        $"\r\n{posi} near '{ambient}'"

    new(message: string, ctx: RuleContext) = ParserError($"{message} on \n\n{CreatePositionInfo(ctx)}")
    new(message: string, errorNode: IErrorNode) = ParserError($"{message} on \n\n{CreatePositionInfo(errorNode)}")
    new(message: string, line: int, column: int) = ParserError($"{message} \n\nCheck\n\n [line:{line}, column:{column}]")
