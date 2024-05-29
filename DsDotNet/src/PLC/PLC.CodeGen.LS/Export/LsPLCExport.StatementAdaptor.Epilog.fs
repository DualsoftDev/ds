namespace PLC.CodeGen.LS

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module XgxTypeConvertorModule =
    type CommentedStatement with
        /// (Commented Statement) To (Commented Statements)
        ///
        /// S -> [XS]
        member internal x.ToCommentedStatements (prjParam: XgxProjectParams, newLocalStorages: XgxStorage) : CommentedStatements =
            let (CommentedStatement(comment, statement)) = x
            let originalComment = statement.ToText()
            let augs = Augments(newLocalStorages, StatementContainer())
            let newStatement = statement.DistributeNegate()
            let newStatement = newStatement.FunctionToAssignStatement prjParam augs
            let newStatement = newStatement.AugmentXgiFunctionParameters prjParam augs

            match prjParam.TargetType with
            | XGI -> newStatement.ToStatements(prjParam, augs)
            | XGK -> newStatement.ToStatementsXgk(prjParam, augs)
            | _ -> failwith "Not supported runtime target"

            let rungComment =
                [
                    comment
                    if prjParam.AppendDebugInfoToRungComment then
                        let statementComment = originalComment  // newStatement.ToText()
                        statementComment
                ] |> ofNotNullAny |> String.concat "\r\n"
                |> escapeXml

            CommentedStatements(rungComment, augs.Statements.ToFSharpList())

