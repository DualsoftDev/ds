namespace Engine.Cpu

open Engine.Core
open System
open System.Linq
open Engine.Parser.FS
open System.Collections.Generic
open System.Collections.Concurrent

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

        member x.GetSourceStorages() =
            match x with
            | DuAssign (expr, target) -> expr.StorageArguments
            | DuVarDecl (expr, var) -> expr.StorageArguments
            | DuTimer timerStatement ->
                [ for s in timerStatement.Timer.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
            | DuCounter counterStatement ->
                [ for s in counterStatement.Counter.InputEvaluateStatements do
                    yield! s.GetSourceStorages() ]
