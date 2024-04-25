namespace PLC.CodeGen.LS


open Engine.Core

[<AutoOpen>]
module LsPLCExportExpressionModule =
    type ExpressionTransformers = {
        TerminalHandler: int*IExpression -> IExpression
        FunctionHandler: int*IExpression -> IExpression
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

                //| Some terminal, None-> $"{space}Terminal: { terminal.ToString() }"
                | None, Some fn ->
                    [
                        $"{space}Function: {fn}"
                        for a in exp.FunctionArguments do
                            traverse (level + 1) a
                    ] |> String.concat "\r\n"
            traverse 0 exp


        /// Expression 에 대해, 주어진 transformer 를 적용한 새로운 expression 을 반환한다.
        /// Expression 을 순환하면서, terminal 에 대해서는 TerminalHandler 를, function 에 대해서는 FunctionHandler 를 적용한다.
        member exp.Transform(tfs:ExpressionTransformers) : IExpression =
            let {TerminalHandler = th; FunctionHandler = fh} = tfs
            let psedoFunction (_args: Args) : bool =
                failwith "THIS IS PSEUDO FUNCTION.  SHOULD NOT BE EVALUATED!!!!"

            let rec traverse (level:int) (exp:IExpression) =
                match exp.Terminal, exp.FunctionName with
                | Some terminal, None -> th (level, exp)
                | None, Some fn ->                     
                    let newArgs = [for a in exp.FunctionArguments do traverse (level + 1) a]
                    let newFn =
                        let newFn = DuFunction { FunctionBody=psedoFunction; Name=fn; Arguments=newArgs }
                        fh (level, newFn)
                    newFn
            traverse 0 exp
