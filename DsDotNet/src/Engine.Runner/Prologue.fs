namespace Engine.Runner

open Engine.Core
open Engine.Common.FS
open System.Threading.Tasks


[<AutoOpen>]
module PrologueModule =
    let noop() = ()

    type WriteResult = Async<unit>  //Task    // unit

    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> WriteResult

    type DoStatus = SegmentBase -> WriteResult
    let private defaultDoStatus(seg:SegmentBase) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

    let mutable doEnqueueAsync:Cpu->(IBit * bool * obj)->WriteResult =
        fun cpu bc -> failwith "Should be overriden"

