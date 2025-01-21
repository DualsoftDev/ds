namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS;
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open System
open System.Linq
open System.Collections.Generic
open System.Reactive.Subjects



[<AutoOpen>]
module DsProgressEvent =

    type ProgressParam = | Progress of Time:DateTime * pro:int

    let mutable private _currentProgress:int = 0
    let GetCurrentProgress() = _currentProgress
    let ProgressSubject = new Subject<ProgressParam>()

    let MarkProgress  (progress:int) =
        assert( 0 <= progress && progress <= 100)
        _currentProgress <- progress
        ProgressSubject.OnNext(Progress (DateTime.Now, progress))



[<AutoOpen>]
module CpuExtensionsModule =

    let private updateStorageValues (sys: DsSystem) tagKind value =
        sys.TagManager.Storages
            .Where(fun w -> w.Value.TagKind = (int)tagKind)
            .Iter(fun t -> t.Value.BoxedValue <- value)

    let private preAction (sys: DsSystem, bAuto: bool) =
        // Update auto and drive button values
        updateStorageValues sys SystemTag.auto_btn bAuto
        updateStorageValues sys SystemTag.drive_btn bAuto

        // Update manual button values
        let bManual = not bAuto
        updateStorageValues sys SystemTag.manual_btn bManual
        //updateStorageValues sys SystemTag.home_btn bManual

        // Update ready button values
        updateStorageValues sys SystemTag.ready_btn true

    ///사용자 autoStartTags HMI 대신 눌러주기
    let preAutoDriveAction(sys:DsSystem) =
        preAction (sys , true)

    ///사용자 manualStartTags HMI 대신 눌러주기
    let preManualAction(sys:DsSystem) =
        preAction (sys , false)


    type ExpressionExt =
        [<Extension>] static member GetChangedTags (xs:IStorage seq) = xs |> Seq.where(fun w -> w.TagChanged)

        [<Extension>]
        static member ClearChangedTags (xs:IStorage seq, systems:DsSystem seq) =
            xs
            |> Seq.where(fun w ->  systems.Any(fun s-> s:>ISystem = w.DsSystem))//자신 시스템에서만 TagChanged  <- false 가능
            |> Seq.iter(fun w -> w.TagChanged <- false)

        [<Extension>]
        static member ExecutableStatements (xs:IStorage seq, mRung:IDictionary<IStorage, Statement seq>) =
            xs
            |> Seq.collect(fun stg -> mRung[stg])
            |> Seq.distinct
