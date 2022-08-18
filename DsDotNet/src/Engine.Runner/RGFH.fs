namespace Engine.Runner

open Engine.Core
open Engine.Common
open System
open System.Linq
open System.Reactive.Linq
open Dual.Common
open System.Collections.Generic


[<AutoOpen>]
module RGFHModule =

    type Writer = IBit*bool*obj -> unit


    type DoStatus = Writer*SegmentBase -> unit
    let private defaultDoStatus(write:Writer, seg:SegmentBase) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

