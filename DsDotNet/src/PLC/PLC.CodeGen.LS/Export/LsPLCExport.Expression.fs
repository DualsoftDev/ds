namespace PLC.CodeGen.LS


open Engine.Core

[<AutoOpen>]
module LsPLCExportExpressionModule =
    type ExpressionTransformers = {
        TerminalHandler: int*IExpression -> IExpression
        FunctionHandler: int*IExpression*IStorage option -> IExpression     // (level, expression, resultStore) -> new expression
    }

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
