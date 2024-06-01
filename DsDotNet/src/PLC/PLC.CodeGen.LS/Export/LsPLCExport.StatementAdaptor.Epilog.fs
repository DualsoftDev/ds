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

            match statement with
            | DuVarDecl(exp, var) ->
                var.Comment <- statement.ToText()                
                var.BoxedValue <- exp.BoxedEvaluatedValue
                augs.Storages.Add var
            | _ -> ()

            match statement with
            | DuVarDecl _ when prjParam.TargetType = XGI ->
                ()
            | _ ->
                let pack = 
                    let kvs:array<string*obj> =
                        [|
                            ("projectParameter", prjParam)
                            ("augments", augs)
                        |]
                    kvs |> DynamicDictionary

                let newStatement = statement.DistributeNegate(pack)
                let newStatement = newStatement.FunctionToAssignStatement(pack)
                let newStatement = newStatement.AugmentXgiFunctionParameters(pack)

                match prjParam.TargetType with
                | XGI -> newStatement.ToStatements(pack)
                | XGK -> newStatement.ToStatementsXgk(pack)
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

