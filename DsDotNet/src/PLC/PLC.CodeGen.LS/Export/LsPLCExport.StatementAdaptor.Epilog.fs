namespace PLC.CodeGen.LS

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

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
            let createPack (prjParam:XgxProjectParams) (augs:Augments) : DynamicDictionary =
                let kvs:array<string*obj> =
                    [|
                        ("projectParameter", prjParam)
                        ("augments", augs)
                    |]
                kvs |> DynamicDictionary

            let pack = createPack prjParam augs

            match statement with
            | DuVarDecl(exp, var) ->
                // 변수 선언문에서 정확한 초기 값 및 주석 값을 가져온다.
                // Local/Global 공유되는 변수에 대해, global 변수가 parser context 에서 부정확한 주석을 얻으므로, 추후에 이를 보정하기 위함이다.
                // - GenerateXmlDocument @ LsPLCExport.Export.fs 참고
                var.Comment <- statement.ToText()     
                let eval = exp.BoxedEvaluatedValue
                var.BoxedValue <- eval
                augs.Storages.Add var

                match prjParam.TargetType with
                | XGK ->
                    DuAssign(Some fake1OnExpression, any2expr eval, var)
                    |> augs.Statements.Add
                    //let exp = exp.ApplyNegate()
                    //let visitor (expPath:IExpression list) (ex:IExpression) : IExpression =
                    //    match ex.FunctionName, expPath with
                    //    | Some (IsOpC op), _  when op.IsOneOf("==", "!=", "<>") && ex.FunctionArguments[0].DataType = typeof<bool> ->
                    //        let ex = ex.AugmentXgk(pack, Some fake1OnExpression, None)
                    //        let auto = prjParam.CreateAutoVariableWithFunctionExpression(pack, ex)
                    //        auto.ToExpression()
                    //    | Some ("&&" | "||" | "!"), []
                    //    | Some (IsOpABC _), _ ->
                    //        let auto = prjParam.CreateAutoVariableWithFunctionExpression(pack, ex)
                    //        auto.ToExpression()
                    //    | _ -> ex
                    //let exp = exp.Visit([], visitor)

                    //match exp.FunctionName, exp.Terminal with
                    //| Some _, None ->
                    //    DuAssign(Some fake1OnExpression, exp, var)
                    //| None, Some _ ->
                    //    DuAction (DuCopy (fake1OnExpression, exp, var))
                    //| _ -> failwith "ERROR"
                    //|> augs.Statements.Add

                | XGI -> () // XGI 에서는 변수 선언에 해당하는 부분을 변수의 초기값으로 할당하고 끝내므로, 더이상의 ladder 생성을 하지 않는다.
                | _ -> failwith "Not supported runtime target"
            | _ ->
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

