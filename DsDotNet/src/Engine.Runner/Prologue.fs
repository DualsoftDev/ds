namespace Engine.Runner

open Engine.Core


[<AutoOpen>]
module PrologueModule =

    type Writer = IBit*bool*obj -> unit


    type DoStatus = Writer*SegmentBase -> unit
    let private defaultDoStatus(write:Writer, seg:SegmentBase) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

