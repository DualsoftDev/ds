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
            | DuAssign (_expr, (:? RisingCoil as rc))  -> [ rc.Storage]
            | DuAssign (_expr, (:? FallingCoil as fc)) -> [ fc.Storage]
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
        static member NotifyStatus (s:ISystem, x:IStorage) =
            match x.GetVertexTagKind() with
            | Some tk ->
                let v = x.Target.Value :?> Vertex
                if (x :?> PlanVar<bool>).Value
                then
                    match tk with
                    | VertexTag.ready  -> onStatusChanged (s, v, Ready)
                    | VertexTag.going  -> onStatusChanged (s, v, Going)
                    | VertexTag.finish -> onStatusChanged (s, v, Finish)
                    | VertexTag.homing -> onStatusChanged (s, v, Homing)
                    | _->()

            | None -> ()

        [<Extension>]
        static member NotifyValue (sys:ISystem, stg:IStorage, newValue:obj) =
            match stg with
            | :? PlanVar<bool> as _p -> onValueChanged (sys, stg, newValue)
            | :? Tag<bool> -> ()//hmi ?
            | _ -> ()

        [<Extension>]
        static member IsEndThread (x:IStorage) =
            match x.GetApiTagKind() with  //외부 시스템 관련 신호
            | Some _ -> true
            | _ ->
                match x.GetVertexTagKind() with
                //EndPortTag  일 경우 새로운 thread 생성
                | Some VertexTag.endPort -> true
              //  | Some VertexTag.homing -> true  /// Homing 인과 H/S 필요??
                | _ -> false

