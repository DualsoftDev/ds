namespace Engine.Core
open System
open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Engine.Common.FS
open System.Diagnostics

[<AutoOpen>]
module ExpressionSerializeModule =
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

    let serializeFunctionExpression (name:string) (args:Args) =
        let args = args |> List.map (fun arg -> (arg :?> IExpression).ToText())
        let args = args |> String.concat ","
        $"{name}({args})"

