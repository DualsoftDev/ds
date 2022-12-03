namespace rec Engine.Core
open System.Diagnostics

[<AutoOpen>]
module ExpressionExtensionModule =
    [<DebuggerDisplay("{ToText()}")>]
    type Statement<'T> =
        | Assign of expression:Expression<'T> * target:IStorage<'T>

        member x.Do() =
            match x with
            | Assign (expr, target) -> target.Value <- expr.Evaluate()

        member x.ToText() =
            match x with
            | Assign (expr, target) -> $"assign({expr.ToText(false)}, {target.ToText()})"



