namespace PLC.CodeGen.LS

open System.Linq
open System.Security

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

[<AutoOpen>]
module XgxTypeConvertorModule =
    /// S -> [XS]
    let internal statement2Statements
        (prjParam: XgxProjectParams)
        (newLocalStorages: XgxStorage)
        (CommentedStatement(comment, statement))
      : CommentedXgxStatements =
        let augs = Augments(newLocalStorages, StatementContainer())
        let statement = statement.MakeExpressionsFlattenizable()
        match prjParam.TargetType with
        | XGI -> statement2XgiStatements prjParam augs statement
        | XGK -> statement2XgkStatements prjParam augs statement
        | _ -> failwith "Not supported runtime target"

        let rungComment =
            [
                comment
                if prjParam.AppendDebugInfoToRungComment then
                    let statementComment = statement.ToText()
                    statementComment
            ] |> ofNotNullAny |> String.concat "\r\n"
            |> escapeXml

       
        CommentedXgiStatements(rungComment, augs.Statements.ToFSharpList())
