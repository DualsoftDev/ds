namespace Engine.Runner

open Engine.Core


[<AutoOpen>]
module PrologueModule =
    let noop() = ()
    type Writer = IBit*bool*obj -> unit


    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange array -> unit

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
            match box bit with
            | :? PortInfoEnd as ep ->
                [|  EndPortChange(ep.Plan, value, cause, onError) :> BitChange
                    if ep.Cpu.IsActive && ep.Actual <> null then
                        BitChange(ep.Actual, value, $"Active CPU endport: writing actual {ep}={value}") |]
            | :? PortInfo -> failwith "Unexpected"
            | _ -> [| BitChange(bit, value, cause, onError) |]
            |> writer
