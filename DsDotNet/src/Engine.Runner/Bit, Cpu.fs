namespace Engine.Runner

open System
open System.Reactive.Disposables

open Dual.Common
open Engine.Core
open Engine.OPC
open System.Threading



[<AutoOpen>]
module BitModule =
    /// bit 변경에 따라 다음 수행해야 할 작업을 찾아서 수행
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
                evaluatePort port newValue
            | _ ->
                failwith "ERROR"


[<AutoOpen>]
module CpuModule =
    let runCpu (cpu:Cpu) = Disposable.Empty

    /// 외부에서 tag 가 변경된 경우 수행할 작업 지정
    let onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
        let tagName, value = tagChange.TagName, tagChange.Value
        if (cpu.TagsMap.ContainsKey(tagName)) then
            let tag = cpu.TagsMap[tagName]
            tag.Value <- value;      // setter 에서 BitChangedSubject.OnNext 가 호출된다.

    /// CPU 별 bit change event queue 에 들어 있는 event 를 처리 : evaluateBit 호출
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

    /// OPC Server 에서 Cpu 가 가지고 있는 tag 값들을 읽어 들임
    /// Engine 최초 구동 시, 수행됨.
    let internal readTagsFromOpc (cpu:Cpu) (opc:OpcBroker) =
        let tpls = opc.ReadTags(cpu.TagsMap.map(fun t -> t.Key))
        for tName, value in tpls do
            let tag = cpu.TagsMap[tName]
            if tag.Value <> value then
                onOpcTagChanged cpu (new OpcTagChange(tName, value))


