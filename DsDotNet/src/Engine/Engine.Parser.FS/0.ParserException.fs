namespace Engine.Parser.FS

open System
open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Dual.Common.Core.FS

type ParserException(message: string) =
    inherit Exception(message)

    static let CreatePositionInfo (ctx: obj) = // RuleContext or IErrorNode
        let getPosition (ctx: obj) =
            let fromToken (token: IToken) = $"{token.Line}:{token.Column}"

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
        $"{posi} near\r\n'{ambient}'"

    new(message: string, ctx: RuleContext) = ParserException($"{message} on {CreatePositionInfo(ctx)}")
    new(message: string, errorNode: IErrorNode) = ParserException($"{message} on {CreatePositionInfo(errorNode)}")
    new(message: string, line: int, column: int) = ParserException($"{message} \nCheck\n line:{line} column:{column}")
