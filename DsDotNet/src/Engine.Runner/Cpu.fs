namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Disposables
open System.Threading
open System.Runtime.CompilerServices
open System.Collections.Generic

open Engine.Common
open Engine.Common.FS
open Engine.Core
open System.Threading.Tasks

[<AutoOpen>]
module CpuModule =

    let doApplyBitChange(bitChange:BitChange) =
        let bit = bitChange.Bit :?> Bit
        //LogDebug($"\t=({indent}) Applying bitchange {bitChange}")

        if bit.Name = "ResetPlan_L_F_Main" then
            noop()

        let mutable bitChanged = false
        match box bit with
        | :? IBitWritable as writable ->
            if bit.Value <> bitChange.NewValue then
                if bit.Name = "ResetPlan_L_F_Main" then
                    noop()
                logDebug $"Writing bit {bit} = {bitChange.NewValue} @ {bit.Cpu.Name}"
                writable.SetValue(bitChange.NewValue)
                bitChanged <- true
        | :? PortInfo ->
            assert(bit.Value = bitChange.NewValue)
            bitChanged <- true
        | _ ->
            assert false

        if bitChanged then
            if (bit :? Tag) then
                let cpu = bitChange.Bit.Cpu
                logDebug($"Publishing tag from CPU[{cpu.Name}]: {bit.Name}={bitChange.NewValue} by {bitChange.CauseRepr}");
                Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(bit.Name, bitChange.NewValue))
            Global.RawBitChangedSubject.OnNext(bitChange)

    let doApply(bitChange:BitChange) =
        if bitChange.BeforeAction <> null then
            bitChange.BeforeAction.Invoke()
        doApplyBitChange(bitChange)

        if bitChange.AfterAction <> null then
            bitChange.AfterAction.Invoke()

    let rec collectForwards(cpu:Cpu, bit:IBit) =
        let fwd = cpu.ForwardDependancyMap
        seq {
            if fwd.ContainsKey(bit) then
                let dependents = fwd[bit]
                for d in dependents do
                    if not <| d :? Expression then
                        yield d

                    if not <| d :? IBitWritable then
                        yield! collectForwards(cpu, d)
        }

    let apply (cpu:Cpu) (bitChange:BitChange) (withQueue:bool) =
        //if (bitChange.Bit.Value == bitChange.NewValue)
        //    return

        //LogDebug($"\t\t=[{cpu.DbgNestingLevel}] Applying bitChange {bitChange}")   // {bitChange.Guid}

        let fwd = cpu.ForwardDependancyMap
        let q = cpu.Queue

        assert(cpu.FFSetterMap <> null)
        cpu.BuildFlipFlopMapOnDemand()

        cpu.DbgNestingLevel <- cpu.DbgNestingLevel + 1
        let bit = bitChange.Bit :?> Bit

        if fwd.ContainsKey(bit) then
            let dependents = collectForwards(cpu, bit).ToArray()
            let prevValues = dependents.Select(fun b -> b, b.Value) |> Tuple.toDictionary


            // 실제 변경 적용
            doApply bitChange

            // 변경으로 인한 파생 변경 enqueue
            let getValue(dep:IBit) =
                match dep with
                | :? BitReEvaluatable as re -> re.Evaluate()
                | _ -> dep.Value

            for dep in dependents do
                let mutable bc:BitChange = null
                match dep with
                | :? PortInfo as pi ->
                    if bit:>IBit = pi.Plan then
                        //Debug.Assert(pi.Plan.Value != pi.Actual?.Value)
                        if (isNull pi.Actual || pi.Plan.Value = pi.Actual.Value) then
                            bc <- BitChange(dep, bitChange.NewValue, pi.Plan)
                        else
                            noop()
                    elif (bit = pi.Actual) then
                        if pi.Plan.Value = pi.Actual.Value then
                            bc <- new BitChange(dep, bitChange.NewValue, pi.Actual)
                        else
                            noop()
                | _ ->
                    ()


                if isNull bc then
                    let newValue = getValue(dep)
                    if newValue <> prevValues[dep] then
                        BitChange(dep, newValue, null) |> doApply 
                else
                    bc |> doApply 
            if bitChange.NewValue then
                if cpu.FFSetterMap.ContainsKey(bit) then
                    for ff in cpu.FFSetterMap[bit].Where(fun ff -> not ff.Value) do
                        BitChange(ff, true, bit) |> doApply 

                if cpu.FFResetterMap.ContainsKey(bit) then
                    for ff in cpu.FFResetterMap[bit].Where(fun ff -> ff.Value) do
                        BitChange(ff, false, bit) |> doApply 
        else
            //LogWarn($"Failed to find dependency for {bit.GetName()}")
            doApply bitChange

        cpu.DbgNestingLevel <- cpu.DbgNestingLevel - 1



    let runCpu(cpu:Cpu) =
        cpu.Apply <- apply cpu

        let disposable = new CancellationDisposable()
        let q = cpu.Queue

        cpu.BuildFlipFlopMapOnDemand()

        let waitHandle = new AutoResetEvent(false)
        let lasts = Dictionary<IBit, bool>()
        let thread =
            Thread(fun () ->
                cpu.DbgThreadId <- Thread.CurrentThread.ManagedThreadId
                logDebug $"\tRunning {cpu.ToText()}"

                cpu.Queue.Added <-
                    Action(fun () ->
                        waitHandle.Set() |> ignore)

                while ( not disposable.IsDisposed && cpu.Running) do
                    waitHandle.WaitOne(TimeSpan.FromMilliseconds(50)) |> ignore
                    while (q.Count > 0 && cpu.Running) do
                        cpu.ProcessingQueue <- true
                        match q.TryDequeue() with
                        | true, bitChange ->
                            let bit = bitChange.Bit
                            assert(bit.Cpu = cpu)
                            let value = bitChange.NewValue
                            if (bit.GetName() = "ResetPlan_L_F_Main") then
                                noop()

                            if lasts.ContainsKey(bit) then
                                if lasts[bit] = value then
                                    logWarn $"Skipping duplicated bit change {bit}={value} @ {cpu.Name}.";
                                    noop();
                                else
                                    apply cpu bitChange true
                                    lasts[bit] <- value;
                            else
                                lasts.Add(bit, value);
                                apply cpu bitChange true

                            bitChange.TCS.SetResult(true)

                        | false, _ ->
                            logWarn $"Failed to deque."
                            failwith "ERROR"

                    cpu.ProcessingQueue <- false
            )

        thread.Start()
        disposable


    let buildBitDependencies(cpu:Cpu) =
        assert(cpu.ForwardDependancyMap.IsNullOrEmpty())
        assert(cpu.BackwardDependancyMap.IsNullOrEmpty())

        cpu.BackwardDependancyMap <- Dictionary<IBit, HashSet<IBit>>()

        let bwd = cpu.BackwardDependancyMap
        let fwd = cpu.ForwardDependancyMap

        let addRelationship(slave:IBit, master:IBit) =
            if not <| fwd.ContainsKey(slave) then
                fwd[slave] <- new HashSet<IBit>()
            fwd[slave].Add(master) |> ignore

            if not <| bwd.ContainsKey(master) then
                bwd[master] <- new HashSet<IBit>()
            bwd[master].Add(slave) |> ignore

        let rec addSubRelationship(bit:IBit) =
            match bit with
            | :? Flag
            | :? Tag ->
                ()
            | :? FlipFlop as ff ->
                addRelationship(ff.S, ff)
                addSubRelationship(ff.S)
                addRelationship(ff.R, ff)
                addSubRelationship(ff.R)
            | :? PortInfo as pi ->
                for mb in pi._monitoringBits.Where(fun b -> b <> null) do
                    addRelationship(mb, pi)
                    addSubRelationship(mb)
            | :? BitReEvaluatable as bre ->
                for mb in bre._monitoringBits.Where(fun b -> b <> null) do
                    addRelationship(mb, bre)
                    addSubRelationship(mb)
            | _ ->
                failwith "ERROR"

        let grp = cpu.BitsMap.Values.GroupByToDictionary(fun b -> b :? BitReEvaluatable)
        if grp.ContainsKey(true) then
            let stems = grp[true].Cast<BitReEvaluatable>()
            for stem in stems do
                addSubRelationship(stem)

        let ffs = cpu.BitsMap.Values.OfType<FlipFlop>()
        for ff in ffs do
            addSubRelationship(ff)

[<Extension>] // type Segment =
type CpuExt =
    [<Extension>] static member Run(cpu:Cpu) = runCpu cpu
    [<Extension>] static member Apply(cpu:Cpu, bitChange:BitChange, withQueue:bool) = apply cpu bitChange withQueue

    /// <summary> Bit 의 값 변경 처리를 CPU 에 위임.  즉시 수행되지 않고, CPU 의 Queue 에 추가 된 후, CPU thread 에서 수행된다.  </summary>
    [<Extension>]
    static member Enqueue(cpu:Cpu, bitChange:BitChange) : WriteResult =
        assert (bitChange.Bit.Cpu = cpu)
        //assert( bitChange.Bit.Value <> bitChange.NewValue)
        if bitChange.Bit.GetName() = "ResetPlan_L_F_Main" then
            noop()

        match bitChange.Bit with
        | :? Expression
        | :? BitReEvaluatable as re when not (re :? PortInfo) ->
            failwith "ERROR: Expression can't be set!"
        | _ ->
            cpu.Queue.Enqueue(bitChange)
            bitChange.TCS.Task

    [<Extension>] static member Enqueue(cpu:Cpu, bit:IBit, newValue:bool, cause:obj) =
                    BitChange(bit, newValue, cause) |> cpu.Enqueue
    [<Extension>] static member Enqueue(cpu:Cpu, bit:IBit, newValue:bool) =
                    BitChange(bit, newValue, null)  |> cpu.Enqueue

    [<Extension>] static member BuildBitDependencies(cpu:Cpu) = buildBitDependencies cpu


