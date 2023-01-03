namespace Engine.Core
open System.Collections.Generic
open Engine.Common.FS

[<AutoOpen>]
module rec ExpressionSerializeModule =
    // 우선 순위가 높을 수록 precedence 값 자체는 작다.
    let operatorPrecedenceMap =
        let dic = Dictionary<string, int>()
        let defs =
            [
                let mutable i = 1
                let incr() = i <- i + 1
                // 우선 순위 최상위 연산자가 맨 처음에 오도록 리스팅
                (["!"; ], i)                      ; incr()
                (["*"; "/"; "%"], i)              ; incr()
                (["+"; "-"], i)                   ; incr()

                (["<<" ; "<<<" ; ">>" ; ">>>"], i); incr()
                ([">" ; ">=" ; "<" ; "<=";], i)   ; incr()
                (["=" ; "!=";], i)                ; incr()
                (["&" ; "&&&";], i)               ; incr()   // bitwise and   (C++/F# style)
                (["^" ; "^^^";], i)               ; incr()   // bitwise xor
                (["|" ; "|||";], i)               ; incr()   // bitwise or
                (["&&";], i)                      ; incr()   // logical AND
                (["||";], i)                      ; incr()   // logical OR
            ]

        for (names, i) in defs do
            for name in names do
                dic.Add(name, i)
        dic

    let isBinaryOperator: (string -> bool)  =
        let hash =
            [
                "*"; "/"; "%"
                "+"; "-"
                "<<" ; "<<<" ; ">>" ; ">>>"
                ">" ; ">=" ; "<" ; "<=";
                "=" ; "!=";
                "&" ; "&&&";   // bitwise and   (C++/F# style)
                "^" ; "^^^";   // bitwise xor
                "|" ; "|||";   // bitwise or
                "&&";          // logical AND
                "||";          // logical OR
            ] |> HashSet<string>
        fun (name:string) -> hash.Contains (name)


    let serializeFunctionNameAndBoxedArguments (name:string) (args:Args) (withParenthesys:bool) =
        let isBinary = isBinaryOperator name
        if isBinary && args.Length = 2 then
            (* 2 + (3 * 4) => sea:'+', island:'*' *)
            let needParenthesys (seaName:string) (island:Arg) =
                option {
                    let! islandName = island.FunctionName
                    if operatorPrecedenceMap.ContainsKey(seaName) && operatorPrecedenceMap.ContainsKey(islandName) then
                        let nSea = operatorPrecedenceMap.[seaName]
                        let nIsland = operatorPrecedenceMap.[islandName]
                        let needParen = nIsland > nSea        // precedence 값이 큰 것이 우선 순위가 낮다.
                        return needParen
                } |> Option.defaultValue false

            let lWithParnethesys = needParenthesys name args[0]
            let rWithParnethesys = needParenthesys name args[1]
            let l = args[0].ToText(lWithParnethesys)
            let r = args[1].ToText(rWithParnethesys)
            let text = $"{l} {name} {r}"
            if withParenthesys then $"({text})" else text
        else
            let args =
                [   for a in args do
                    let withParenthesys = args.Length >= 2
                    a.ToText(withParenthesys)
                ] |> String.concat ", "
            $"{name}({args})"

