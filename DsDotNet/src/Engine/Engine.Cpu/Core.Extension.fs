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
            match x.GetVertexTagKind() with
            | Some tk ->
                let v = (x :> IStorage).Target.Value :?> Vertex
                match tk with
                | VertexTag.ready  -> onStatusChanged (v, Ready)
                | VertexTag.going  -> onStatusChanged (v, Going)
                | VertexTag.finish -> onStatusChanged (v, Finish)
                | VertexTag.homing -> onStatusChanged (v, Homing)
                | _->()

            | None -> ()


        [<Extension>]
        static member IsEndThread (x:IStorage) =
            match x.GetVertexTagKind() with
            | Some VertexTag.endPort -> true
         //   | Some VertexTag.goingPulse -> true
            | _ -> false

