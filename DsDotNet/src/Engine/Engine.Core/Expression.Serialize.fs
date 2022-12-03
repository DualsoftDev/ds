namespace Engine.Core
open System.Collections.Generic
open Engine.Common.FS

[<AutoOpen>]
module rec ExpressionSerializeModule =
    let operatorPrecedenceMap =
        let dic = Dictionary<string, int>()
        let defs =
            [
                let mutable i = 1
                let incr() = i <- i + 1
                (["*"; "/"; "mul"; "muld"; "div"; "divd"], i); incr()
                (["+"; "-"; "add"; "addd"; "sub"; "subd"], i); incr()
            ]
        for (names, i) in defs do
            for name in names do
                dic.Add(name, i)
        dic

    let isBinaryFunctionOrOperator  =
        let hash =
            [ "*"; "/"; "mul"; "muld"; "div"; "divd"
              "+"; "-"; "add"; "addd"; "sub"; "subd"
            ] |> HashSet<string>
        fun (name:string) -> hash.Contains (name)


    let serializeFunctionNameAndBoxedArguments (name:string) (args:Args) (withParenthesys:bool) =
        let isBinary = isBinaryFunctionOrOperator name
        if isBinary && args.Length = 2 then
            let precedence = operatorPrecedenceMap[name]
            let l = args[0].ToText(true)
            let r = args[1].ToText(true)
            let text = $"{l} {name} {r}"
            if withParenthesys then $"({text})" else text
        else
            let args =
                [   for a in args do
                    let withParenthesys = args.Length >= 2
                    a.ToText(withParenthesys)
                ] |> String.concat ", "
            $"{name}({args})"

