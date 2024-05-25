namespace PLC.CodeGen.LS

open System.Linq
open System.Security

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

[<AutoOpen>]
module XgxTypeConvertorModule =
    /// (Commented Statement) To (Commented Statements)
    ///
    /// S -> [XS]
    let internal cs2Css
        (prjParam: XgxProjectParams)
        (newLocalStorages: XgxStorage)
        (CommentedStatement(comment, statement))
      : CommentedXgxStatements =
        let augs = Augments(newLocalStorages, StatementContainer())
        let newStatement = statement.MakeExpressionsFlattenizable()
        //let newStatement = newStatement.AugmentXgkArithmeticExpressionToAssignStatemnt prjParam augs
        match prjParam.TargetType with
        | XGI -> s2XgiSs prjParam augs newStatement
        | XGK -> s2XgkSs prjParam augs newStatement
        | _ -> failwith "Not supported runtime target"

        let rungComment =
            [
                comment
                if prjParam.AppendDebugInfoToRungComment then
                    let statementComment = newStatement.ToText()
                    statementComment
            ] |> ofNotNullAny |> String.concat "\r\n"
            |> escapeXml

        CommentedXgiStatements(rungComment, augs.Statements.ToFSharpList())
