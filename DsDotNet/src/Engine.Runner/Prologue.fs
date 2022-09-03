namespace Engine.Runner

open Engine.Core
open Engine.Common.FS
open System.Threading.Tasks


[<AutoOpen>]
module PrologueModule =
    let noop() = ()
    //type Writer = IBit*bool*obj -> unit


    type WriteResult = Task    // unit

    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> WriteResult

    type DoStatus = SegmentBase*ChangeWriter -> WriteResult
    let private defaultDoStatus(seg:SegmentBase, writer:ChangeWriter) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

[<AutoOpen>]
module internal BitWriterModule =
    type BitWriter = IBit * bool * obj -> WriteResult

    let getBitWriter (writer:ChangeWriter) : BitWriter =
        fun (bit:IBit, value, cause) ->
            if value && bit.GetName() = "End_VPS_L_F_Main" then
                noop()

            task {
                match box bit with
                | :? PortInfoEnd as ep ->
                        do! writer(EndPortChange(ep.Plan, value, cause))
                        if ep.Cpu.IsActive && ep.Actual <> null then
                            do! writer(BitChange(ep.Actual, value, $"Active CPU endport: writing actual {ep}={value}"))
                | :? PortInfo ->
                    failwithlog "Unexpected"
                | _ ->
                    do! writer(BitChange(bit, value, cause))
            }
