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
                | DuAssign (cmdExpr, _) ->
                    let code = cmdExpr.ToText()
                    let expr = parseExpression v.Storages code 
                    withExpressionComment comment (DuAssign (expr, v.PEFunc))
                |_ -> failWithLog $"err {comment}"
                )
        ]

    member v.C2_DoCommand() =
        let call = v.Vertex :?> Call
        let comment = getFuncName()
        [
            yield! call.TargetFunc.Statements.Select(fun s->
                    match s with
                    | DuAssign (cmdExpr, target) ->
                    let sets = v.MM.Expr <&&> cmdExpr
                    withExpressionComment comment (DuAssign (sets, target))
                    |_ -> failWithLog $"err {comment}"
                    )
        ]

