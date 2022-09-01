using System.Threading;
using System.Threading.Tasks;

namespace Engine.Core;

/// <summary>
/// Queue 이용한 구현
/// todo: ObservableConcurrentQueue 이용해서 spinning thread 없이 구현 (https://www.nuget.org/packages/ObservableConcurrentQueue/)
/// </summary>
public static class CpuExtensionQueueing
{
    public static IDisposable Run(this Cpu cpu)
    {
        var disposable = new CancellationDisposable();
        var q = cpu.Queue;

        cpu.BuildFlipFlopMapOnDemand();

        AutoResetEvent waitHandle = new AutoResetEvent(false);

        new Thread(() =>
        {
            cpu.DbgThreadId = Thread.CurrentThread.ManagedThreadId;
            Global.Logger.Debug($"\tRunning {cpu.ToText()}");

            cpu.Queue.Added += () =>
            {
                waitHandle.Set();
            };

            while (!disposable.IsDisposed && cpu.Running)
            {
                waitHandle.WaitOne(TimeSpan.FromMilliseconds(50));
                while (q.Count > 0 && cpu.Running)
                {
                    cpu.ProcessingQueue = true;
                    if (q.TryDequeue(out BitChange[] bitChanges))
                    {
                        if (bitChanges[0].Bit.GetName() == "AutoStart_L_F")
                            Global.NoOp();
                        cpu.Applies(bitChanges, true);
                    }
                    else
                    {
                        Global.Logger.Warn($"Failed to deque.");
                        throw new Exception("ERROR");
                    }
                }
                cpu.ProcessingQueue = false;
                //Thread.Sleep(1);
            }
        }).Start()
        ;


        return disposable;
    }

    public static void Applies(this Cpu cpu, BitChange[] bitChanges, bool withQueue)
    {
        //if (bitChange.Bit.Value == bitChange.NewValue)
        //    return;

        //Global.Logger.Debug($"\t\t=[{cpu.DbgNestingLevel}] Applying bitChange {bitChange}");   // {bitChange.Guid}

        var fwd = cpu.ForwardDependancyMap;
        var q = cpu.Queue;

        Debug.Assert(cpu.FFSetterMap != null);

        cpu.DbgNestingLevel++;
        var bits = bitChanges.ToDictionary(bc => bc.Bit, bc => bc);

        if (bits.Keys.Any(b => fwd.ContainsKey(b)))
        {
            var dependents = bits.Keys.SelectMany(b => collectForwards(cpu, b)).Distinct().ToArray();
            var prevValues = dependents.ToDictionary(dep => dep, dep => dep.Value);


            // 실제 변경 적용
            bitChanges.Iter(DoApply);

            // 변경으로 인한 파생 변경 enqueue
            {
                bool getValue(IBit dep) => (dep is BitReEvaluatable re) ? re.Evaluate() : dep.Value;
                List<BitChange> changes = new();
                foreach (var dep in dependents)
                {
                    BitChange bc = null;
                    if (dep is PortInfo pi)
                    {
                        if (bits.ContainsKey(pi.Plan))
                        {
                            //Debug.Assert(pi.Plan.Value != pi.Actual?.Value);
                            if (pi.Actual == null || pi.Plan.Value == pi.Actual.Value)
                                bc = new BitChange(dep, bits[pi.Plan].NewValue, pi.Plan, bits[pi.Plan].OnError);
                            else
                                Global.NoOp();
                        }
                        else if (pi.Actual != null && bits.ContainsKey(pi.Actual))
                        {
                            if (pi.Plan.Value == pi.Actual.Value)
                                bc = new BitChange(dep, bits[pi.Actual].NewValue, pi.Actual, bits[pi.Actual].OnError);
                            else
                                Global.NoOp();
                        }
                    }

                    if (bc == null)
                    {
                        var newValue = getValue(dep);
                        if (newValue != prevValues[dep])
                            bc = new BitChange(dep, newValue, null, null);
                    }
                    if (bc != null)
                        changes.Add(bc);
                }

                foreach (var kv in bits)
                {
                    var (b, bc) = (kv.Key, kv.Value);
                    if (cpu.FFSetterMap.ContainsKey(b) && bc.NewValue)
                        foreach (var ff in cpu.FFSetterMap[b].Where(ff => !ff.Value))
                            changes.Add(new BitChange(ff, true, b, bc.OnError));

                    if (cpu.FFResetterMap.ContainsKey(b) && bc.NewValue)
                        foreach (var ff in cpu.FFResetterMap[b].Where(ff => ff.Value))
                            changes.Add(new BitChange(ff, false, b, bc.OnError));
                }


                foreach (var bc in changes)
                    cpu.Applies(new[] { bc }, withQueue);
            }
        }
        else
        {
            //Global.Logger.Warn($"Failed to find dependency for {bit.GetName()}");
            bitChanges.Iter(DoApply);
        }
        cpu.DbgNestingLevel--;
    }

    //public static void Apply(this Cpu cpu, BitChange bitChange, bool withQueue)
    //{
    //    //if (bitChange.Bit.Value == bitChange.NewValue)
    //    //    return;

    //    Global.Logger.Debug($"\t\t=[{cpu.DbgNestingLevel}] Applying bitChange {bitChange}");   // {bitChange.Guid}

    //    var fwd = cpu.ForwardDependancyMap;
    //    var q = cpu.Queue;

    //    Debug.Assert(cpu.FFSetterMap != null);
    //    cpu.BuildFlipFlopMapOnDemand();

    //    cpu.DbgNestingLevel++;
    //    var bit = (Bit)bitChange.Bit;

    //    if (bitChange.Bit.GetName() == "EndActual_A_F_Sm")  //"ResetPlan_A_F_Sm")  //"StartPlan_A_F_Vm") //"InnerStartSourceFF_VPS_A_F_Pp_Vp")   // "StartPlanAnd_VPS_A_F_Pp")
    //        Global.NoOp();


    //    if (fwd.ContainsKey(bit))
    //    {
    //        var dependents = collectForwards(cpu, bit).ToArray();
    //        var prevValues = dependents.ToDictionary(dep => dep, dep => dep.Value);


    //        // 실제 변경 적용
    //        DoApply(bitChange);

    //        // 변경으로 인한 파생 변경 enqueue
    //        {
    //            bool getValue(IBit dep) => (dep is BitReEvaluatable re) ? re.Evaluate() : dep.Value;
    //            List<BitChange> changes = new();
    //            foreach(var dep in dependents)
    //            {
    //                BitChange bc = null;
    //                if (dep is PortInfo pi)
    //                {
    //                    if (pi.Plan == bit)
    //                    {
    //                        //Debug.Assert(pi.Plan.Value != pi.Actual?.Value);
    //                        if (pi.Actual == null || pi.Plan.Value == pi.Actual.Value)
    //                            bc = new BitChange(dep, bitChange.NewValue, bit, bitChange.OnError);
    //                        else
    //                            Global.NoOp();
    //                    }
    //                    else if (pi.Actual == bit)
    //                    {
    //                        if (pi.Plan.Value == pi.Actual.Value)
    //                            bc = new BitChange(dep, bitChange.NewValue, bit, bitChange.OnError);
    //                        else
    //                            Global.NoOp();
    //                    }
    //                }

    //                if (bc == null)
    //                {
    //                    var newValue = getValue(dep);
    //                    if (newValue != prevValues[dep])
    //                        bc = new BitChange(dep, newValue, bit, bitChange.OnError);
    //                }
    //                if (bc != null)
    //                    changes.Add(bc);
    //            }

    //            if (cpu.FFSetterMap.ContainsKey(bit) && bitChange.NewValue)
    //                foreach (var ff in cpu.FFSetterMap[bit].Where(ff => !ff.Value))
    //                    changes.Add(new BitChange(ff, true, bit, bitChange.OnError));

    //            if (cpu.FFResetterMap.ContainsKey(bit) && bitChange.NewValue)
    //                foreach (var ff in cpu.FFResetterMap[bit].Where(ff => ff.Value))
    //                    changes.Add(new BitChange(ff, false, bit, bitChange.OnError));


    //            foreach (var bc in changes)
    //                cpu.Apply(bc, withQueue);
    //        }
    //    }
    //    else
    //    {
    //        //Global.Logger.Warn($"Failed to find dependency for {bit.GetName()}");
    //        DoApply(bitChange);
    //    }
    //    cpu.DbgNestingLevel--;
    //}

    static void DoApply(BitChange bitChange)
    {
        bitChange.BeforeAction?.Invoke();
        DoApplyBitChange(bitChange);
        bitChange.AfterAction?.Invoke();
    }

    static void DoApplyBitChange(BitChange bitChange)
    {
        var bit = (Bit)bitChange.Bit;
        //Global.Logger.Debug($"\t=({indent}) Applying bitchange {bitChange}");

        var bitChanged = false;
        if (bit is IBitWritable writable)
        {
            if (bit.Value != bitChange.NewValue)
            {
                writable.SetValue(bitChange.NewValue);
                bitChanged = true;
            }
        }
        else if (bit is PortInfo)
        {
            Debug.Assert(bit.Value == bitChange.NewValue);
            bitChanged = true;
        }
        else
            Debug.Assert(false);

        if (bitChanged)
        {
            try
            {
                Global.RawBitChangedSubject.OnNext(bitChange);
                if (bit is Tag tag)
                    Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(bit.Name, bitChange.NewValue));
            }
            catch (Exception ex)
            {
                Global.Logger.Error(ex);
                if (bitChange.OnError == null)
                    throw;
                else
                    bitChange.OnError(ex);
            }
        }
    }

    static IEnumerable<IBit> collectForwards(Cpu cpu, IBit bit)
    {
        var fwd = cpu.ForwardDependancyMap;
        if (fwd.ContainsKey(bit))
        {
            var dependents = fwd[bit];
            foreach (var d in dependents)
            {
                if (d is not Expression)
                    yield return d;

                if (d is not IBitWritable)
                    foreach (var dd in collectForwards(cpu, d))
                        yield return dd;
            }
        }
    }


    /// <summary> Bit 의 값 변경 처리를 CPU 에 위임.  즉시 수행되지 않고, CPU 의 Queue 에 추가 된 후, CPU thread 에서 수행된다.  </summary>
    public static void Enqueue(this Cpu cpu, BitChange bitChange)
    {
        Debug.Assert(bitChange.Bit.Cpu == cpu);

        //Debug.Assert(bitChange.Bit.Value != bitChange.NewValue);

        switch (bitChange.Bit)
        {
            case Expression _:
            case BitReEvaluatable re when re is not PortInfo:
                throw new Exception("ERROR: Expression can't be set!");
            default:
                //var last = cpu.Queue.LastOrDefault();
                //if (last != null && last.Bit == bitChange.Bit && last.NewValue == bitChange.NewValue)
                //    Global.Logger.Warn($"Skipping enque'ing duplicate change {bitChange}");
                //else
                cpu.Queue.Enqueue(new[] { bitChange });
                break;
        };
    }

    public static void Enqueues(this Cpu cpu, BitChange[] bitChanges)
    {
        cpu.Queue.Enqueue(bitChanges);
    }
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        Enqueue(cpu, new BitChange(bit, newValue, cause));
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue) =>
        Enqueue(cpu, new BitChange(bit, newValue, null));


    public static void SendChange(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        SendChange(cpu, new BitChange(bit, newValue, cause));
    public static void SendChange(this Cpu cpu, BitChange bitChange) =>
        cpu.Applies(new[] { bitChange }, false);
    public static void SendChanges(this Cpu cpu, BitChange[] bitChanges) =>
        bitChanges.Iter(bitChange => cpu.Applies(new[] { bitChange }, false));
    public static void PostChange(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        Enqueue(cpu, new BitChange(bit, newValue, cause));

    public static void BuildBitDependencies(this Cpu cpu)
    {
        Debug.Assert(cpu.ForwardDependancyMap.IsNullOrEmpty());
        Debug.Assert(cpu.BackwardDependancyMap.IsNullOrEmpty());

        cpu.BackwardDependancyMap = new();

        var bwd = cpu.BackwardDependancyMap;
        var fwd = cpu.ForwardDependancyMap;

        void addRelationship(IBit slave, IBit master)
        {
            if (!fwd.ContainsKey(slave))
                fwd[slave] = new HashSet<IBit>();
            fwd[slave].Add(master);

            if (!bwd.ContainsKey(master))
                bwd[master] = new HashSet<IBit>();
            bwd[master].Add(slave);
        }

        void addSubRelationship(IBit bit)
        {
            switch(bit)
            {
                case Flag:
                case Tag:
                    break;
                case FlipFlop ff:
                    addRelationship(ff.S, ff);
                    addSubRelationship(ff.S);
                    addRelationship(ff.R, ff);
                    addSubRelationship(ff.R);
                    break;
                case PortInfo pi:
                    foreach (var mb in pi._monitoringBits.Where(b => b != null))
                    {
                        addRelationship(mb, pi);
                        addSubRelationship(mb);
                    }
                    break;
                case BitReEvaluatable bre:
                    foreach (var mb in bre._monitoringBits.Where(b => b != null))
                    {
                        addRelationship(mb, bre);
                        addSubRelationship(mb);
                    }
                    break;
                default:
                    throw new Exception("ERROR");
            }
        }

        var grp = cpu.BitsMap.Values.GroupByToDictionary(b => b is BitReEvaluatable);
        if (grp.ContainsKey(true))
        {
            var stems = grp[true].Cast<BitReEvaluatable>();
            foreach (var stem in stems)
                addSubRelationship(stem);
        }

        var ffs = cpu.BitsMap.Values.OfType<FlipFlop>();
        foreach (var ff in ffs)
            addSubRelationship(ff);
    }
}
