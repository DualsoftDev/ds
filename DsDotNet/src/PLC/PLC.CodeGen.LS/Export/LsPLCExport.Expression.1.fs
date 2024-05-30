namespace PLC.CodeGen.LS


open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.Common

[<AutoOpen>]
module LsPLCExportExpressionModule =
    type ExpressionTransformers = {
        TerminalHandler: int*IExpression -> IExpression
        FunctionHandler: int*IExpression*IStorage option -> IExpression     // (level, expression, resultStore) -> new expression
    }

    type XgxStorage = ResizeArray<IStorage>
    type Augments(storages:XgxStorage, statements:StatementContainer) =
        new() = Augments(XgxStorage(), StatementContainer())
        member val Storages = storages        // ResizeArray<IStorage>
        member val Statements = statements    // ResizeArray<Statement>
        member val ExpressionStore:IStorage option = None with get, set


    type IExpression with
        /// 주어진 Expression 을 multi-line text 형태로 변환한다.
        member exp.ToTextFormat() : string =
            let tab n = String.replicate (n*4) " "
            let rec traverse (level:int) (exp:IExpression) =
                let space = tab level
                match exp.Terminal, exp.FunctionName with
                | Some terminal, None ->
                    match terminal.Variable, terminal.Literal with
                    | Some storage, None -> $"{space}Storage: {storage.ToText()}"
                    | None, Some literal -> $"{space}Literal: {literal.ToText()}"
                    | _ -> failwith "Invalid expression"

                | None, Some fn ->
                    [
                        $"{space}Function: {fn}"
                        for a in exp.FunctionArguments do
                            traverse (level + 1) a
                    ] |> String.concat "\r\n"
                | _ -> failwith "Invalid expression"
            traverse 0 exp

        member exp.Visit (f: IExpression -> IExpression) : IExpression =
            match exp.Terminal, exp.FunctionName with
            | Some _terminal, None ->
                f exp
            | None, Some _fn ->
                let args = exp.FunctionArguments |> map f
                exp.WithNewFunctionArguments args |> f
            | _ ->
                failwith "Invalid expression"

        /// Expression 을 flattern 할 수 있는 형태로 변환 : e.g !(a>b) => (a<=b)
        /// Non-terminal negation 을 terminal negation 으로 변경
        member x.ApplyNegate() : IExpression =
            let exp = x
            let negate (expr:IExpression) : IExpression =
                match expr.Terminal, expr.FunctionName with
                    | Some _terminal, None ->
                        createUnaryExpression "!" expr
                    | None, Some "!" -> expr.FunctionArguments.ExactlyOne()
                    | None, Some _fn -> createUnaryExpression "!" expr
                    | _ -> failwith "Invalid expression"

            let rec visitArgs (negated:bool) (expr:IExpression) : IExpression =
                match expr.Terminal, expr.FunctionName with
                | Some _terminal, None ->
                    match negated with
                    // terminal 의 negation 은 bool type 에 한정한다.
                    | true when expr.DataType = typedefof<bool> -> negate expr
                    | _-> expr
                | None, Some _fn ->
                    visitFunction negated expr
                | _ -> failwith "Invalid expression"

            and visitFunction (negated:bool) (expr:IExpression) : IExpression =
                let args = expr.FunctionArguments
                if negated then
                    match expr.Terminal, expr.FunctionName with
                    | Some _terminal, None ->
                        negate expr
                    | None, Some(IsComparisonOperator fn) ->
                        let newArgs = args |> map (visitArgs true)
                        let reverseFn =
                            match fn with
                            | "==" -> "!="
                            | "!=" | "<>" -> "=="
                            | ">" ->  "<="
                            | ">=" -> "<"
                            | "<" ->  ">="
                            | "<=" -> ">"
                            | _ -> failwith "ERROR"
                        createCustomFunctionExpression reverseFn newArgs
                    | None, Some("&&" | "||" as fn) ->
                        let newArgs = args |> map (visitArgs true)
                        let reverseFn =
                            match fn with
                            | "&&" -> "||"
                            | "||" -> "&&"
                            | _ -> failwith "ERROR"
                        createCustomFunctionExpression reverseFn newArgs
                    | None, Some "!" ->
                        args.ExactlyOne() |> visitFunction false
                    | _ -> failwith "Invalid expression"
                else
                    match expr.Terminal, expr.FunctionName with
                    | Some _terminal, None -> expr
                    | None, Some "!" ->
                        args.ExactlyOne() |> visitArgs true 
                    | None, Some _fn ->
                        let newArgs = args |> map (visitArgs false)
                        expr.WithNewFunctionArguments newArgs
                    | _ -> failwith "Invalid expression"

            visitFunction false exp


        /// Expression 에 대해, 주어진 transformer 를 적용한 새로운 expression 을 반환한다.
        /// Expression 을 순환하면서, terminal 에 대해서는 TerminalHandler 를, function 에 대해서는 FunctionHandler 를 적용한다.
        member exp.Transform(tfs:ExpressionTransformers, resultStore:IStorage option) : IExpression =
            let {TerminalHandler = th; FunctionHandler = fh} = tfs

            let rec traverse (level:int) (exp:IExpression) (resultStore:IStorage option) : IExpression =
                match exp.Terminal, exp.FunctionName with
                | Some _terminal, None -> th (level, exp)
                | None, Some _fn ->
                    let args = exp.FunctionArguments
                    let newArgs = [for a in args do traverse (level + 1) a None]
                    let newFn =
                        let f = exp.WithNewFunctionArguments newArgs
                        fh (level, f, resultStore)
                    newFn
                | _ -> failwith "Invalid expression"
            traverse 0 exp resultStore



