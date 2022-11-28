[<AutoOpen>]
module rec Engine.Cpu.StatementModule

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open Engine.Cpu.FunctionImpl


[<AutoOpen>]
[<DebuggerDisplay("{ToText()}")>]
type Statement<'T> = 
    | Assign      of expression:Expression<'T>   * target:Tag<'T>
    member x.Do() =
        match x with
        | Assign     (expr, target) -> 
                     ///  Target Y = Function (X)
                     target.Data <- evaluate(expr) |> ToData
                     ()
    member x.ToText() =
         match x with
         | Assign     (expr, target) -> $"assign({expr.ToText()}, {target.ToText()})"
