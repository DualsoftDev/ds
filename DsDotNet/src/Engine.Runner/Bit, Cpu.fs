namespace Engine.Runner

open System
open System.Reactive.Disposables

open Dual.Common
open Engine.Core
open Engine.OPC
open System.Threading



[<AutoOpen>]
module internal CpuModule =
    let runCpu (cpu:Cpu) = Disposable.Empty

    /// 외부에서 tag 가 변경된 경우 수행할 작업 지정
    let onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
        let tagName, value = tagChange.TagName, tagChange.Value
        if (cpu.TagsMap.ContainsKey(tagName)) then
            let tag = cpu.TagsMap[tagName]
            if tag.Value <> value then
                tag.Value <- value;      //! setter 에서 BitChangedSubject.OnNext --> onBitChanged 가 호출된다.

    /// CPU 별 bit change event queue 에 들어 있는 event 를 처리 : evaluateBit 호출
    let private processQueue(cpu:Cpu) =
        /// bit 변경에 따라 다음 수행해야 할 작업을 찾아서 수행
        let evaluateBit (causalBit:IBit) (bit:IBit) =
            let evaluateEdge(edge:Edge) =
                if edge.Value <> edge.IsSourcesTrue then
                    logDebug $"\tEvaluating Edge {edge} due to {causalBit}"
                    edge.Value <- edge.IsSourcesTrue
                    edge.TargetTag.Value <- edge.Value
                else
                    logDebug "\t\tSkip evaluating edge %A" edge
                ()

            match bit with
            | :? Edge as e when e.IsRootEdge ->
                evaluateEdge(e)
            | :? Edge as e ->
                logWarn $"Need keep going for internal edge: {e}"
            | _ ->
                let cpu = bit.OwnerCpu
                let prevs = cpu.BackwardDependancyMap[bit]
                let newValue = prevs |> Seq.exists(fun b -> b.Value)
                let current = bit.Value
                if current <> newValue then
                    logDebug $"\tEvaluating bit {bit} due to {causalBit}"
                    match bit with
                    | :? Port as port ->
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
                        assert not bc.Applied
                        bit.Value <- bc.NewValue


                    logDebug "\tProcessing Queue: %A" bc

                    if bit :? IResetEdge then
                        ()

                    if cpu.ForwardDependancyMap.ContainsKey(bit) then
                        cpu.ForwardDependancyMap[bit] |> Seq.iter (evaluateBit bit)
                | false, _ ->
                    goOn <- false

            Thread.Sleep(10)



    let onBitChanged (cpu:Cpu) (bitChange:BitChange) =
        if bitChange.Bit.OwnerCpu = cpu then
            cpu.Queue.Enqueue(bitChange)
            processQueue cpu
        else
            // skipping other cpu's bit change
            ()


