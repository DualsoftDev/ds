using System.Threading;
using System.Threading.Tasks;

namespace Engine.Core;

/// <summary>
/// Queue 이용한 구현
/// </summary>
public static class CpuExtensionQueueing
{
    public static IDisposable Run(this Cpu cpu)
    {
        var disposable = new CancellationDisposable();
        var q = cpu.Queue;

        cpu.BuildFlipFlopMapOnDemand();

        new Thread(async () =>
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            while (!disposable.IsDisposed && cpu.Running)
            {
                while (q.Count > 0 && cpu.Running)
                {
                    cpu.ProcessingQueue = true;
                    if (q.TryDequeue(out BitChange bitChange))
                    {
                        if (bitChange.Bit.GetName() == "AutoStart_L_F")
                            Console.WriteLine();
                        cpu.Apply(bitChange, true);
                    }
                    else
                        Global.Logger.Warn($"Failed to deque.");
                }
                cpu.ProcessingQueue = false;
                await Task.Delay(20);
            }
        }).Start()
        ;


        return disposable;
    }
    public static void Apply(this Cpu cpu, BitChange bitChange, bool withQueue)
    {
        if (bitChange.Bit.GetName() == "Start_A_F_Vp")
            Console.WriteLine();

        Global.Logger.Debug($"\t\t=[{cpu.NestingLevel}] Applying bitChange {bitChange}");   // {bitChange.Guid}

        var fwd = cpu.ForwardDependancyMap;
        var q = cpu.Queue;

        cpu.BuildFlipFlopMapOnDemand();

        cpu.NestingLevel++;
        var bit = (Bit)bitChange.Bit;
        if (fwd.ContainsKey(bit))
        {
            var dependents = fwd[bit].OfType<BitReEvaluatable>();
            var prevValues = dependents.ToDictionary(dep => dep, dep => dep.Value);

            // 실제 변경 적용
            DoApply(bitChange);

            // 변경으로 인한 파생 변경 enqueue
            {
                var changes = (
                        from dep in dependents
                        let newValue = dep.Evaluate()
                        where newValue != prevValues[dep]
                        select (new BitChange(dep, newValue, bit, bitChange.OnError))
                    ).ToList();

                if (cpu.FFSetterMap.ContainsKey(bit) && bitChange.NewValue)
                    foreach (var ff in cpu.FFSetterMap[bit].Where(ff => !ff.Value))
                        changes.Add(new BitChange(ff, true, bit, bitChange.OnError));

                if (cpu.FFResetterMap.ContainsKey(bit) && bitChange.NewValue)
                    foreach (var ff in cpu.FFResetterMap[bit].Where(ff => ff.Value))
                        changes.Add(new BitChange(ff, false, bit, bitChange.OnError));

                var chgrp = changes.GroupByToDictionary(ch => ch.Bit is PortInfo);
                if (chgrp.ContainsKey(false))
                {
                    var nonPorts = chgrp[false];
                    foreach (var bc in nonPorts)
                        cpu.Apply(bc, withQueue);
                }
                if (chgrp.ContainsKey(true))
                {
                    var ports = chgrp[true];
                    foreach (var bc in ports)
                    {
                        var port = (PortInfo)bc.Bit;
                        if (bit == port.Plan)
                        {
                            var newBc = new PortInfoPlanChange(bc);
                            if (withQueue)
                                q.Enqueue(newBc);
                            else
                                cpu.Apply(newBc, withQueue);
                        }
                        else if (bit == port.Actual)
                        {
                            var newBc = new PortInfoActualChange(bc);
                            if (withQueue)
                                q.Enqueue(newBc);
                            else
                                cpu.Apply(newBc, withQueue);

                        }
                        else
                            throw new Exception("ERROR");
                    }
                }
            }
        }
        else
        {
            //Global.Logger.Warn($"Failed to find dependency for {bit.GetName()}");
            DoApply(bitChange);
        }
        cpu.NestingLevel--;


        void DoApply(BitChange bitChange)
        {
            var bit = (Bit)bitChange.Bit;
            //Global.Logger.Debug($"\t=({indent}) Applying bitchange {bitChange}");

            // bit 가 나의 cpu 의 bit 가 아닌 경우, 타 cpu 에서 수행할 수 있도록 tag 변경을 공지.
            // e.g.  Call 의 TX 에 해당하는 bit 변경은 Call 이 정의된 system 의 cpu 에서 처리한다.
            if (! cpu.BitsMap.ContainsKey(bit.Name))
            {
                if (bit is Tag)
                    Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(bit.Name, bitChange.NewValue));
                else
                    throw new Exception("ERROR");
            }


            var bitChanged = bitChange switch
            {
                PortInfoPlanChange pc => pc.PortInfo.PlanValueChanged(pc.NewValue),
                PortInfoActualChange ac => ac.PortInfo.ActualValueChanged(ac.NewValue),
                _ => new Func<bool>(() =>
                {
                    if (bit is IBitWritable writable)
                    {
                        writable.SetValue(bitChange.NewValue);
                        return true;
                    }
                    else
                    {
                        Debug.Assert(bit.Value == bitChange.NewValue);
                        return false;
                    }
                })(),
            };

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

    }


    /// <summary> Bit 의 값 변경 처리를 CPU 에 위임.  즉시 수행되지 않고, CPU 의 Queue 에 추가 된 후, CPU thread 에서 수행된다.  </summary>
    public static void Enqueue(this Cpu cpu, BitChange bitChange)
    {
        switch (bitChange.Bit)
        {
            case Expression _:
            case BitReEvaluatable re when re is not PortInfo:
                throw new Exception("ERROR: Expression can't be set!");
            default:
                cpu.Queue.Enqueue(bitChange);
                break;
        };
    }
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        Enqueue(cpu, new BitChange(bit, newValue, cause));
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue) =>
        Enqueue(cpu, new BitChange(bit, newValue, null));


    public static void SendChange(this Cpu cpu, IBit bit, bool newValue, object cause) =>
        SendChange(cpu, new BitChange(bit, newValue, cause));
    public static void SendChange(this Cpu cpu, BitChange bitChange) =>
        cpu.Apply(bitChange, false);
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
                case BitReEvaluatable bre:
                    foreach (var mb in bre._monitoringBits.Where(b => b != null))
                    {
                        addRelationship(mb, bre);
                        addSubRelationship(mb);
                    }
                    break;
                case FlipFlop ff:
                    addRelationship(ff.S, ff);
                    addSubRelationship(ff.S);
                    addRelationship(ff.R, ff);
                    addSubRelationship(ff.R);
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
        //var terminals = grp[false];


        var ffs = cpu.BitsMap.Values.OfType<FlipFlop>();
        foreach (var ff in ffs)
            addSubRelationship(ff);
    }

    //public static IEnumerable<IBit> CollectForwardDependantBits(this Cpu cpu, IBit source)
    //{
    //    var fwd = cpu.ForwardDependancyMap;
    //    if (!fwd.ContainsKey(source))
    //        yield break;

    //    foreach (var dep in cpu.ForwardDependancyMap[source])
    //    {
    //        yield return dep;
    //        foreach (var v in cpu.CollectForwardDependantBits(dep))
    //            yield return v;
    //    }
    //}


    abstract class PortInfoChange : BitChange
    {
        public PortInfo PortInfo { get; }
        public PortInfoChange(BitChange bc)
            : base(bc.Bit, bc.NewValue, bc.Cause, bc.OnError)
        {
            PortInfo = (PortInfo)bc.Bit;
        }
    }
    class PortInfoPlanChange : PortInfoChange
    {
        public PortInfoPlanChange(BitChange bc) : base(bc) {}
    }
    class PortInfoActualChange : PortInfoChange
    {
        public PortInfoActualChange(BitChange bc) : base(bc) { }
    }
}
