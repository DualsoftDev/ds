namespace Engine.Cpu

open Engine.Core

[<AutoOpen>]
module CoreExtensionsModule =
    type Statement with
        member x.GetTargetStorages() =
            match x with
            | DuAssign (expr, target) -> [ target ]
            | DuVarDecl (expr, var) -> [ var ]
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuAction (DuCopy (condition, source, target)) -> [ target ]
            | DuAugmentedPLCFunction _ -> []

        member x.GetSourceStorages() =
            match x with
            | DuAssign (expr, target) -> expr.CollectStorages()
            | DuVarDecl (expr, var) -> expr.CollectStorages()
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuAction (DuCopy (condition, source, target)) -> condition.CollectStorages()
            | DuAugmentedPLCFunction _ -> []
