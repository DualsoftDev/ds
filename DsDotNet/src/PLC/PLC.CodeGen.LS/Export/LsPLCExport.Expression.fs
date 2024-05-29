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

                //| Some terminal, None-> $"{space}Terminal: { terminal.ToString() }"
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
                    | Some terminal, None ->
                        createUnaryExpression "!" expr
                    | None, Some "!" -> expr.FunctionArguments.ExactlyOne()
                    | None, Some fn -> createUnaryExpression "!" expr
                    | _ -> failwith "Invalid expression"

            let rec visitArgs (negated:bool) (expr:IExpression) : IExpression =
                match expr.Terminal, expr.FunctionName with
                | Some terminal, None ->
                    match negated with
                    | true -> createUnaryExpression "!" expr
                    | false -> expr
                | None, Some fn ->
                    //let negated = negated <> (fn = "!")
                    visitFunction negated expr

            and visitFunction (negated:bool) (expr:IExpression) : IExpression =
                if negated then
                    match expr.Terminal, expr.FunctionName with
                    | Some terminal, None ->
                        negate expr//.FunctionArguments.ExactlyOne()
                    | None, Some(IsComparisonOperator fn) ->
                        let args = expr.FunctionArguments |> map (visitArgs false)
                        let reverseFn =
                            match fn with
                            | "==" -> "!="
                            | "!=" -> "=="
                            | ">" ->  "<="
                            | ">=" -> "<"
                            | "<" ->  ">="
                            | "<=" -> ">"
                        createCustomFunctionExpression reverseFn args
                    | None, Some("&&" | "||" as fn) ->
                        let args = expr.FunctionArguments |> map (visitArgs true)
                        let reverseFn = if fn = "&&" then "||" else "&&"
                        createCustomFunctionExpression reverseFn args
                        //| "&&" -> DuFunction { FunctionBody = fLogicalOr; Name = "||"; Arguments = args } :> IExpression
                        //| "||" -> DuFunction { FunctionBody = fLogicalAnd; Name = "&&"; Arguments = args } :> IExpression
                    | None, Some "!" ->
                        expr.FunctionArguments.ExactlyOne() |> visitFunction false
                else
                    match expr.Terminal, expr.FunctionName with
                    | Some terminal, None -> expr
                    | None, Some "!" ->
                        let args = expr.FunctionArguments |> map (visitArgs true)
                        args.ExactlyOne()
                    | None, Some fn ->
                        let args = expr.FunctionArguments |> map (visitArgs false)
                        expr.WithNewFunctionArguments args
                    
                
            //let rec visit (parentNegated:bool) (expr:IExpression) : IExpression =
            //    match expr.Terminal, expr.FunctionName with
            //    | Some terminal, None ->
            //        match parentNegated with
            //        | true -> fLogicalNot([expr])
            //        | false -> expr
            //    | None, Some fn ->
            //        let negated = parentNegated <> (fn = "!")
            //        let newExp =
            //            if negated then
            //                if fn = "!" then
            //                    negate (expr.FunctionArguments.ExactlyOne())
            //                else
            //                    let args = expr.FunctionArguments |> map (fun ex -> negate ex |> visit (not negated))
            //                    match fn with
            //                    | "!" -> negate expr
            //                    | "&&" -> DuFunction { FunctionBody = fLogicalOr; Name = "||"; Arguments = args } :> IExpression
            //                    | "||" -> DuFunction { FunctionBody = fLogicalOr; Name = "&&"; Arguments = args } :> IExpression

            //                    | "==" -> DuFunction { FunctionBody = fNotEqual; Name = "!="; Arguments = args } :> IExpression
            //                    | "!=" -> DuFunction { FunctionBody = fEqual;    Name = "=="; Arguments = args } :> IExpression
            //                    | ">" ->  DuFunction { FunctionBody = fLt;       Name = "<="; Arguments = args } :> IExpression
            //                    | ">=" -> DuFunction { FunctionBody = fLte;      Name = "<";  Arguments = args } :> IExpression
            //                    | "<" ->  DuFunction { FunctionBody = fGt;       Name = ">="; Arguments = args } :> IExpression
            //                    | "<=" -> DuFunction { FunctionBody = fGte;      Name = ">";  Arguments = args } :> IExpression

            //                    | _ -> DuFunction { FunctionBody = fLogicalNot; Name = "!"; Arguments = args } :> IExpression
            //            else
            //                expr

            //        //let negated = if fn = "!" then not parentNegated else parentNegated
            //        //let negated = parentNegated
            //        match newExp.Terminal, newExp.FunctionName with
            //        //| Some terminal, None when negated ->
            //        //    DuFunction { FunctionBody = fLogicalNot; Name = "!"; Arguments = [newExp] } :> IExpression                        
            //        | Some terminal, None ->
            //            newExp
            //        | None, Some "!" ->
            //            let args = newExp.FunctionArguments |> map (visit (not parentNegated))
            //            args.ExactlyOne()
            //        | None, Some fn ->
            //            let args = newExp.FunctionArguments |> map (visit (parentNegated))
            //            newExp.WithNewFunctionArguments args
            //    | _ ->
            //        failwith "Invalid expression"
            //visit false exp

            let xxx = visitFunction false exp
            xxx

        ///// 1. Expression 내의 비교 연산을 임시 변수로 할당하고 대체
        //member exp.MakeFlattenizable(): IExpression =
        //    exp.DistributeNegate()


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



