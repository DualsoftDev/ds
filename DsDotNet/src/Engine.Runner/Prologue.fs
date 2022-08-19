namespace Engine.Runner

open Engine.Core


[<AutoOpen>]
module PrologueModule =

    type Writer = IBit*bool*obj -> unit


    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> unit

    type DoStatus = SegmentBase*ChangeWriter*ExceptionHandler -> unit
    let private defaultDoStatus(seg:SegmentBase, writer:ChangeWriter, exceptionHandler:ExceptionHandler) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

