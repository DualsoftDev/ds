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
        let newStatement1 = statement.DistributeNegate()
        let originalComment = newStatement1.ToText()
        let newStatement2 = newStatement1.AugmentXgkArithmeticExpressionToAssignStatemnt prjParam augs
        let newStatement2 = newStatement2.AugmentXgiFunctionParameters prjParam augs

        match prjParam.TargetType with
        | XGI -> s2XgiSs prjParam augs newStatement2
        | XGK -> s2XgkSs prjParam augs newStatement2
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

