[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFunctions

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open Engine.Parser.FS


type VertexMCall with

  
    member v.C1_DoOperator() =
        let call = v.Vertex :?> Call
        let comment = getFuncName()
        let sts = call.TargetFunc.Statements
        if sts.Count = 1
        then 
            [
            match sts.Head() with
            | DuAssign (_, cmdExpr, _) ->
                let code = cmdExpr.ToText()
                let expr = parseExpression v.Storages code 
                yield withExpressionComment comment (DuAssign (None, expr, v.CallOperatorValue))
            |_ -> failWithLog $"err {comment}"
            ]
        elif sts.Count > 1
        then failwithlog $"Operator({call.Name})에는 하나의 수식이 필요합니다. \r\n테이블 정의 수식 Count:({sts.Count})"
        else []
         

    member v.C2_DoCommand() =
        let call = v.Vertex :?> Call
        let comment = getFuncName()
        [
            if call.TargetFunc.Statements.any() 
            then
                yield! call.TargetFunc.Statements |> Seq.collect(fun s->
                        [
                            match s with
                            | DuAssign (_, cmdExpr, target) ->
                            let sets = v.MM.Expr :> IExpression<bool> 
                            yield withExpressionComment comment (DuAssign (sets|> Some, cmdExpr, target))
                            yield withExpressionComment comment (DuAssign (None, sets, v.CallCommandEnd))
                            |_ -> failWithLog $"err {comment}"
                        ]
                    )
            else
                yield withExpressionComment comment (DuAssign (None, v.MM.Expr, v.CallCommandEnd))
        ]

