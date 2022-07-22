namespace Engine.Runner

open System
open System.Reactive.Disposables

open Dual.Common
open Engine.Core
open Engine.OPC
open System.Threading


[<AutoOpen>]
module SegmentModule =
    let rec goingChild (child:Child) =
        match child.Coin with
        | :? ExSegmentCall as extSeg ->
            //goingSegment extSeg
            ()
        | :? SubCall as call->
            //goingCall call
            ()
        | _ ->
            failwith "ERROR"

        child.TagsStart |> Seq.iter(fun t -> t.Value <- true)

    and private goingSegment (seg:Segment) =
        assert seg.PortS.Value

        // 1. Ready 상태에서의 clean start
        // 2. Going pause (==> Ready 로 해석) 상태에서의 resume start

        let allFinished = seg.IsChildrenStatusAllWith(Status4.Finished)
        if allFinished then
            seg.PortE.Value <- true
            assert(seg.Status = Status4.Finished)
        else
            let anyHoming = seg.IsChildrenStatusAnyWith(Status4.Homing)
            if anyHoming then
                assert(seg.IsChildrenStatusAllWith(Status4.Homing))      // 하나라도 homing 이면, 모두 homing
                if seg.IsChildrenOrigin() then
                    let map = seg.ChildStatusMap
                    let keys = map.Keys |> Array.ofSeq
                    for key in keys do
                        map[key] <- Status4.Ready

            let allReady = seg.IsChildrenStatusAllWith(Status4.Ready)
            let anyGoing = seg.IsChildrenStatusAnyWith(Status4.Going)
            if (allReady || anyGoing) then
                if allReady then
                    // do origin check
                    ()

                let v_oes = seg.TraverseOrder
                for ve in v_oes do
                    let child = ve.Vertex :?> Child
                    let es = ve.OutgoingEdges
                    match child.Status with
                    // child call 을 "잘" 시켜야 한다.
                    | Status4.Ready ->
                        goingChild child
                    | Status4.Going
                    | Status4.Finished ->
                        ()
                    | _ ->
                        failwith "ERROR"


            if (seg.IsChildrenStatusAnyWith(Status4.Homing)) then
                ()


    let private homing() = ()
    let private pause() = ()
    let private finish() = ()
    let private ready() = ()

    let evaluatePort (seg:Segment) (port:Port) (newValue:bool) =
        if port.Value <> newValue then

            let rf = seg.IsResetFirst
            let st = seg.Status

            // start port 와 reset port 동시 눌림
            let duplicate =
                newValue &&
                    match port with
                    | :? PortS when seg.PortR.Value -> true
                    | :? PortR when seg.PortS.Value -> true
                    | _ -> false


            let mutable effectivePort = port
            if duplicate then
                effectivePort <- if rf then seg.PortR :> Port else seg.PortS


            effectivePort.Value <- newValue
            match effectivePort, newValue, st with
            | :? PortS, true , Status4.Ready -> goingSegment seg
            | :? PortS, false, Status4.Ready -> pause()
            | :? PortR, true , Status4.Finished -> homing()
            | :? PortR, false, Status4.Finished -> pause()
            | :? PortR, true , Status4.Going -> homing()
            | :? PortR, false, Status4.Going -> pause()
            | :? PortE, true , Status4.Going -> finish()
            | :? PortE, false, Status4.Homing -> ready()
            | :? PortR, true , Status4.Ready -> ()
            | :? PortR, false, Status4.Ready ->
                    if seg.PortS.Value then
                        goingSegment seg
            | :? PortS, true,  Status4.Finished -> ()
            | :? PortS, false, Status4.Finished ->
                    if seg.PortR.Value then
                        homing()

            | _ ->
                failwith "ERROR"



[<AutoOpen>]
module BitModule =
    let evaluateBit (bit:IBit) =
        let cpu = bit.OwnerCpu
        let prevs = cpu.BackwardDependancyMap[bit]
        let newValue = prevs |> Seq.exists(fun b -> b.Value)
        let current = bit.Value
        if current <> newValue then
            match bit with
            | :? Flag
            | :? Tag ->
                ()
            | :? Port as port ->
                let seg = port.OwnerSegment
                evaluatePort seg port newValue
            | _ ->
                failwith "ERROR"


[<AutoOpen>]
module CpuModule =
    let runCpu (cpu:Cpu) = Disposable.Empty

    /// <summary> 외부에서 tag 가 변경된 경우 </summary>
    let onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
        let tagName, value = tagChange.TagName, tagChange.Value
        if (cpu.TagsMap.ContainsKey(tagName)) then
            let tag = cpu.TagsMap[tagName]
            tag.Value <- value;      // setter 에서 BitChangedSubject.OnNext 가 호출된다.

    let processQueue(cpu:Cpu) =
        let mutable bc:BitChange = null;
        while (cpu.Queue.Count > 0) do
            let mutable goOn = true
            while goOn do
                match cpu.Queue.TryDequeue() with
                | true, bc ->
                    let bit = bc.Bit
                    if (bc.NewValue <> bit.Value) then
                        assert not bc.Applied
                        bit.Value <- bc.NewValue

                    if cpu.ForwardDependancyMap.ContainsKey(bit) then
                        cpu.ForwardDependancyMap[bit] |> Seq.iter evaluateBit
                | false, _ ->
                    goOn <- false

            Thread.Sleep(10)

    let onBitChanged (cpu:Cpu) (bitChange:BitChange) =
        cpu.Queue.Enqueue(bitChange)
        processQueue cpu

    let internal readTagsFromOpc (cpu:Cpu) (opc:OpcBroker) =
        let tpls = opc.ReadTags(cpu.TagsMap.map(fun t -> t.Key))
        for tName, value in tpls do
            let tag = cpu.TagsMap[tName]
            if tag.Value <> value then
                onOpcTagChanged cpu (new OpcTagChange(tName, value))

[<AutoOpen>]
module EngineModule =

    type Engine(model:Model, opc:OpcBroker, activeCpu:Cpu) =
        let cpus = model.Cpus
        member x.Model = model
        member x.Opc = opc
        member x.Cpu = activeCpu


        member x.Run() =
            let subscriptions =
                [
                    for cpu in cpus do
                        Global.BitChangedSubject.Subscribe(onBitChanged cpu)


                    // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
                    Global.OpcTagChangedSubject
                        .Subscribe(fun tc ->
                            cpus
                            |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName))
                            |> Seq.iter(fun cpu -> onOpcTagChanged cpu tc))

                    for cpu in cpus do
                        readTagsFromOpc cpu opc
                        runCpu cpu
                ]

            new CompositeDisposable(subscriptions)

