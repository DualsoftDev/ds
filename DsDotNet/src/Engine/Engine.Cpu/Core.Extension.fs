namespace Engine.Cpu

open Engine.Core
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
[<AutoOpen>]
module CoreExtensionsModule =
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



    let getTargetStorages (x:Statement) = x.GetTargetStorages() |> List.toSeq
    let getSourceStorages (x:Statement) = x.GetSourceStorages() |> List.toSeq
    let getAutoButtons (x:DsSystem) = x.AutoButtons

    [<Extension>]
    type ExpressionExt =
        [<Extension>]
        static member NotifyStatus (x:PlanVar<bool>) =
            if x.Vertex.IsSome
            then            //마지막 문자 한글자만 추출 tagname_R_  -> R
                let m = Regex.Match(x.Name, "(?<=_)\D(?=_$)")
                match m.Value with
                | "R" -> onStatusChanged (x.Vertex.Value, Ready)
                | "G" -> onStatusChanged (x.Vertex.Value, Going)
                | "F" -> onStatusChanged (x.Vertex.Value, Finish)
                | "H" -> onStatusChanged (x.Vertex.Value, Homing)
                | _->()


