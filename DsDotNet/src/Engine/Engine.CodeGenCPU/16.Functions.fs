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
        [
            yield! call.TargetFunc.Statements.Select(fun s->
                match s with
                | DuAssign (_, cmdExpr, _) ->
                    let code = cmdExpr.ToText()
                    let expr = parseExpression v.Storages code 
                    withExpressionComment comment (DuAssign (None, expr, v.PEFunc))
                |_ -> failWithLog $"err {comment}"
                )
        ]

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
                            yield withExpressionComment comment (DuAssign (None, sets, v.PEFunc))
                            |_ -> failWithLog $"err {comment}"
                        ]
                    )
            else
                yield withExpressionComment comment (DuAssign (None, v.MM.Expr, v.PEFunc))
        ]

