namespace Engine.Cpu

open Engine.Core
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open System

[<AutoOpen>]
module CoreExtensionsModule =
    type Statement with
        member x.GetTargetStorages() =
            match x with
            | DuAssign (_expr, target) -> [ target ]
            | DuVarDecl (_expr, var) -> [ var ]
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetTargetStorages() ]
            | DuAction (DuCopy (_condition, _source, target)) -> [ target ]
            | DuAugmentedPLCFunction _ -> []

        member x.GetSourceStorages() =
            match x with
            | DuAssign (expr, _target) -> expr.CollectStorages()
            | DuVarDecl (expr, _var) -> expr.CollectStorages()
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuAction (DuCopy (condition, _source, _target)) -> condition.CollectStorages()
            | DuAugmentedPLCFunction _ -> []



    let getTargetStorages (x:Statement) = x.GetTargetStorages() |> List.toSeq
    let getSourceStorages (x:Statement) = x.GetSourceStorages() |> List.toSeq
    let getAutoButtons (x:DsSystem) = x.AutoButtons

    [<Extension>]
    type ExpressionExt =
        [<Extension>]
        static member NotifyStatus (x:PlanVar<bool>) =
            let stg = (x :> IStorage)
            if stg.Target.IsSome && (stg.Target.Value :? Vertex)
            then
                let tagKind = Enum.ToObject(typeof<VertexTag>,  stg.TagKind) :?> VertexTag
                let vertex  = stg.Target.Value :?> Vertex
                match tagKind with
                | VertexTag.ready  -> onStatusChanged (vertex, Ready)
                | VertexTag.going  -> onStatusChanged (vertex, Going)
                | VertexTag.finish -> onStatusChanged (vertex, Finish)
                | VertexTag.homing -> onStatusChanged (vertex, Homing)
                | _->()


