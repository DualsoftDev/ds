namespace Engine.Runner

open Engine.Core


[<AutoOpen>]
module PrologueModule =
    let noop() = ()
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

[<AutoOpen>]
module internal BitWriterModule =
    type BitWriter = IBit * bool * obj -> unit

    let getBitWriter (writer:ChangeWriter) onError =
        fun (bit, value, cause) ->
            assert(not <| box bit :? PortInfo)
            writer(BitChange(bit, value, cause, onError))

    let getEndPortPlanWriter (writer:ChangeWriter) onError =
        fun (endPort:PortInfoEnd, value, cause) ->
            //assert(box endPort:? PortInfo)
            writer(PortInfoPlanChange(BitChange(endPort, value, cause, onError)))
