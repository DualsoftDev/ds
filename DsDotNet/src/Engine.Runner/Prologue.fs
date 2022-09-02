namespace Engine.Runner

open Engine.Core
open Engine.Common.FS


[<AutoOpen>]
module PrologueModule =
    let noop() = ()
    type Writer = IBit*bool*obj -> unit


    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> unit

    type DoStatus = SegmentBase*ChangeWriter -> unit
    let private defaultDoStatus(seg:SegmentBase, writer:ChangeWriter) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

[<AutoOpen>]
module internal BitWriterModule =
    type BitWriter = IBit * bool * obj -> unit

    let getBitWriter (writer:ChangeWriter) =
        fun (bit:IBit, value, cause) ->
            if value && bit.GetName() = "End_VPS_L_F_Main" then
                noop()

            match box bit with
            | :? PortInfoEnd as ep ->
                writer(EndPortChange(ep.Plan, value, cause))
                if ep.Cpu.IsActive && ep.Actual <> null then
                    writer(BitChange(ep.Actual, value, $"Active CPU endport: writing actual {ep}={value}"))
            | :? PortInfo -> failwithlog "Unexpected"
            | _ -> writer(BitChange(bit, value, cause))
