namespace PLC.CodeGen.LS


open Engine.Core
open Dual.Common.Core.FS

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



        member exp.CollectOperators () : string list =
            [
                match exp.Terminal, exp.FunctionName with
                | Some _terminal, None ->
                    ()
                | None, Some fn ->
                    yield fn
                    yield! exp.FunctionArguments |> collect (fun arg -> arg.CollectOperators())
                | _ ->
                    failwith "Invalid expression"
            ]
        member exp.IsPureBoolean() : bool = exp.CollectOperators() |> Seq.forall(fun op -> op.IsOneOf("&&", "||", "!"))

        member exp.Visit (f: IExpression -> IExpression) : IExpression =
            match exp.Terminal, exp.FunctionName with
            | Some _terminal, None ->
                f exp
            | None, Some _fn ->
                let args = exp.FunctionArguments |> map f
                exp.WithNewFunctionArguments args |> f
            | _ ->
                failwith "Invalid expression"

        /// Non-terminal negation 을 terminal negation 으로 변경
        member exp.TerminalizeNegate() : IExpression =
            match exp.Terminal, exp.FunctionName with
            | Some terminal, None -> exp
            | None, Some fn ->
                let args = exp.FunctionArguments |> map (fun a -> a.TerminalizeNegate())
                if fn = "!" then
                    let arg0 = args.ExactlyOne()
                    let subArgs = arg0.FunctionArguments
                    match arg0.FunctionName with
                    | Some "!" -> arg0
                    | Some "==" -> DuFunction { FunctionBody = fNotEqual; Name = "!="; Arguments = subArgs } :> IExpression
                    | Some "!=" -> DuFunction { FunctionBody = fEqual;    Name = "=="; Arguments = subArgs } :> IExpression
                    | Some ">" ->  DuFunction { FunctionBody = fLt;       Name = "<";  Arguments = subArgs } :> IExpression
                    | Some ">=" -> DuFunction { FunctionBody = fLte;      Name = "<="; Arguments = subArgs } :> IExpression
                    | Some "<" ->  DuFunction { FunctionBody = fGt;       Name = ">";  Arguments = subArgs } :> IExpression
                    | Some "<=" -> DuFunction { FunctionBody = fGte;      Name = ">="; Arguments = subArgs } :> IExpression

                    | _ -> exp
                else
                    exp.WithNewFunctionArguments args
            | _ ->
                failwith "Invalid expression"

        /// Expression 을 flattern 할 수 있는 형태로 변환
        /// 1. Non-terminal negation 을 terminal negation 으로 변경
        /// 1. Expression 내의 비교 연산을 임시 변수로 할당하고 대체
        member exp.MakeFlattenizable (augs:Augments) : IExpression =
            exp.TerminalizeNegate()


        /// Expression 에 대해, 주어진 transformer 를 적용한 새로운 expression 을 반환한다.
        /// Expression 을 순환하면서, terminal 에 대해서는 TerminalHandler 를, function 에 대해서는 FunctionHandler 를 적용한다.
        member exp.Transform(tfs:ExpressionTransformers, resultStore:IStorage option) : IExpression =
            let {TerminalHandler = th; FunctionHandler = fh} = tfs

            let rec traverse (level:int) (exp:IExpression) (resultStore:IStorage option) =
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
