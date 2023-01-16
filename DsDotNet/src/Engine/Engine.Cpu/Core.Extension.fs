namespace Engine.Cpu

open Engine.Core
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreExtensionsModule =
    // <ahn> Engine.Cpu 에서만 사용하는 확장 모듈이므로 Engine.Core 로 옮길 필요는 없습니다.
    type Statement with
        member x.GetTargetStorages() =
            match x with
            | DuAssign (expr_, target) -> [ target ]
            | DuVarDecl (expr_, var) -> [ var ]
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuAction (DuCopy (condition_, source_, target)) -> [ target ]
            | DuAugmentedPLCFunction _ -> []

        member x.GetSourceStorages() =
            match x with
            | DuAssign (expr, target_) -> expr.CollectStorages()
            | DuVarDecl (expr, var_) -> expr.CollectStorages()
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuAction (DuCopy (condition, source_, target_)) -> condition.CollectStorages()
            | DuAugmentedPLCFunction _ -> []



    let getTargetStorages (x:Statement) = x.GetTargetStorages()
    let getSourceStorages (x:Statement) = x.GetSourceStorages()
    let getAutoButtons (x:DsSystem) = x.AutoButtons


//[<Extension>]
//type StatementExt =
//    [<Extension>] static member GetTargetStorages (x:Statement) = x.GetTargetStorages()
//    [<Extension>] static member GetSourceStorages (x:Statement) = x.GetSourceStorages()
