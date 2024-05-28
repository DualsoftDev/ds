namespace PLC.CodeGen.LS

open Engine.Core
open Dual.Common.Core.FS

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
        let newStatement = statement.DistributeNegate()
        let originalComment = newStatement.ToText()
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

        CommentedXgiStatements(rungComment, augs.Statements.ToFSharpList())

