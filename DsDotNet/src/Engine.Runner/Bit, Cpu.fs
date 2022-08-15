#nowarn "760"   // warning FS0760: IDisposable 인터페이스를 지원하는 개체는 생성 값이 리소스를 소유할 수도 있다는 것을 표시하기 위해 생성자를 나타내는 함수 값으로 'Type(args)' 또는 'Type'이 아니라 'new Type(args)' 구문을 사용하여 만드는 것이 좋습니다.

namespace Engine.Runner

open System
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Threading
open System.Collections.Generic
open System.Runtime.CompilerServices

open Dual.Common
open Engine.Core
open System.Collections.Concurrent
open System.Linq

//[<Extension>] // type Segment =
//type EngineExt =
//    [<Extension>]
//    static member OnSomethingWithSegment(segment:Segment, status:Status4) =
//        noop()

[<AutoOpen>]
module internal CpuModule =
    let runCpu (cpu:Cpu) =
        let cancels = ConcurrentDictionary<Segment, CancellationTokenSource>()
        let onSegmentStatusChanged (seg:Segment) (status:Status4) =
            let doReady (seg:Segment) _cts =
                ()
            let doGoing (seg:Segment) (cts:CancellationTokenSource) =
                ()
            let doFinish (seg:Segment) _cts =
                // just confirm finish
                noop()
            let doHoming (seg:Segment) (cts:CancellationTokenSource) =
                ()

            match cancels.TryGetValue(seg) with
            | true, cts -> cts.Cancel()
            | _ -> ()

            (seg, new CancellationTokenSource())
            ||> match status with
                | Status4.Ready    -> doReady
                | Status4.Going    -> doGoing
                | Status4.Finished -> doFinish
                | Status4.Homing   -> doHoming
                | _  -> failwith "ERROR"

        let listenPortChanges (segment:Segment) =
            let getSegmentStatus portS portR portE =
                match portS, portR, portE with
                | false, false, false -> Status4.Ready  //??
                | true, false, false -> Status4.Going
                | _, false, true -> Status4.Finished
                | _, true, true -> Status4.Homing
                | _ -> failwith "Unexpected"

            let bits = segment.GetAllPorts() |> Enumerable.Cast<IBit> |> HashSet
            let subs =
                Global.BitChangedSubject
                    .Where(fun bc -> bits.Contains(bc.Bit))
                    .Subscribe (fun bc ->
                        noop()
                        //! todo
                        (*
                            let portS = segment.PortStartBits |> Seq.exists(fun b -> b.Value)
                            let portR = segment.PortResetBits |> Seq.exists(fun b -> b.Value)
                            let portE = segment.PortEndBits   |> Seq.exists(fun b -> b.Value)
                            let segStatus = getSegmentStatus portS portR portE
                            assert (segStatus <> previousSegmentStatus)
                            //Global.SegmentStatusChangedSubject.OnNext(SegmentStatusChange(segment, segStatus))
                            onSegmentStatusChanged segment segStatus
                            *)
                        ())
            segment.Disposables.Add(subs)
            subs

        //cpu.ForwardDependancyMap.Clear();
        //if cpu.BackwardDependancyMap <> null then
        //    cpu.BackwardDependancyMap.Clear();

        //cpu.BuildBitDependencies()

        [
            cpu.Run()
            for f in cpu.RootFlows do
            for s in f.RootSegments do
                listenPortChanges s
        ] |> CompositeDisposable

    /// 외부에서 tag 가 변경된 경우 수행할 작업 지정
    let onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
        let tagName, value = tagChange.TagName, tagChange.Value
        if (cpu.TagsMap.ContainsKey(tagName)) then
            let tag = cpu.TagsMap[tagName]
            if tag.Value <> value then
                cpu.Enqueue(tag, value, $"OPC Tag 변경");      //! setter 에서 BitChangedSubject.OnNext --> onBitChanged 가 호출된다.

    /// CPU 별 bit change event queue 에 들어 있는 event 를 처리 : evaluateBit 호출
    let private processQueue(cpu:Cpu) =
        assert(false)
        /// bit 변경에 따라 다음 수행해야 할 작업을 찾아서 수행
        let evaluateBit (causalBit:IBit) (bit:IBit) =
            let evaluateEdge(edge:Edge) =
                if edge.Value <> edge.IsSourcesTrue then
                    logDebug $"\tEvaluating Edge {edge} due to {causalBit}"
                    edge.Value <- edge.IsSourcesTrue
                    edge.TargetTag.SetValue(edge.Value)
                else
                    logDebug "\t\tSkip evaluating edge %A" edge
                ()

            match bit with
            | :? Edge as e when e.IsRootEdge ->
                evaluateEdge(e)
            | :? Edge as e ->
                logWarn $"Need keep going for internal edge: {e}"
            | _ ->
                let cpu = bit.Cpu
                let prevs = cpu.BackwardDependancyMap[bit]
                let newValue = prevs |> Seq.exists(fun b -> b.Value)
                let current = bit.Value
                if current <> newValue then
                    logDebug $"\tEvaluating bit {bit} due to {causalBit}"
                    match bit with
                    | :? PortInfo as port ->
                        evaluatePort port newValue
                    | :? Flag
                    | :? Tag ->
                        failwith "ERROR"

                    | _ ->
                        failwith "ERROR"
                else
                    logDebug "\t\tSkip evaluating bit %A" bit


        while (cpu.Queue.Count > 0) do
            let mutable goOn = true
            while goOn do
                match cpu.Queue.TryDequeue() with
                | true, bc ->
                    let bit = bc.Bit
                    if (bc.NewValue <> bit.Value) then
                        match bit with
                        | :? IBitWritable as wb -> wb.SetValue(bc.NewValue)
                        | _ -> assert (bit.Value = bc.NewValue)


                    logDebug "\tProcessing Queue: %A" bc

                    if bit :? IResetEdge then
                        ()

                    if cpu.ForwardDependancyMap.ContainsKey(bit) then
                        cpu.ForwardDependancyMap[bit] |> Seq.iter (evaluateBit bit)
                | false, _ ->
                    goOn <- false

            Thread.Sleep(10)



    let onBitChanged (cpu:Cpu) (bitChange:BitChange) =
        if bitChange.Bit.Cpu = cpu then
            //cpu.Queue.Enqueue(bitChange)
            //processQueue cpu
            noop()
        else
            // skipping other cpu's bit change
            ()


