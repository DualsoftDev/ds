namespace Engine.Runner

open Engine.Core
open Engine.Common.FS
open System.Threading.Tasks


[<AutoOpen>]
module PrologueModule =
    let noop() = ()

    type WriteResult = Async<unit>  //Task    // unit

    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> Task  // WriteResult

    type DoStatus = SegmentBase -> WriteResult
    let private defaultDoStatus(seg:SegmentBase) =
        failwith "Should be overriden"

    let mutable doReady:DoStatus = defaultDoStatus
    let mutable doGoing:DoStatus = defaultDoStatus
    let mutable doFinish:DoStatus = defaultDoStatus
    let mutable doHoming:DoStatus = defaultDoStatus

    let mutable doEnqueueAsync:Cpu->BitChange->Task =
        fun cpu bc -> failwith "Should be overriden"

[<AutoOpen>]
module internal BitWriterModule =
    type BitWriter = IBit * bool * obj -> WriteResult

    let getBitWriter (cpu:Cpu) : BitWriter =
        let writer x = doEnqueueAsync cpu x |> Async.AwaitTask
        fun (bit:IBit, value, cause) ->
            async {
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
