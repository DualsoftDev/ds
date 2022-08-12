using System.Collections.Concurrent;
using System.Reactive.Joins;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.Core;

public class Cpu : Named, ICpu
{
    public IEngine Engine { get; set; }
    public Model Model { get; }

    /// <summary> My System 의 Cpu 인지 여부</summary>
    public bool IsActive { get; set; }



    /// <summary> this Cpu 가 관장하는 root flows </summary>
    public List<RootFlow> RootFlows { get; } = new();

    /// <summary> Bit change event queue </summary>
    public ConcurrentQueue<BitChange> Queue { get; } = new();

    /// <summary>CPU queue 에 더 처리할 내용이 있음을 외부에 알리기 위한 flag</summary>
    public bool ProcessingQueue { get; internal set; }
    /// <summary>외부에서 CPU 를 멈추거나 가동하기 위한 flag</summary>
    public bool Running { get; set; } = true;
    public bool NeedWait => Running && (ProcessingQueue || Queue.Count > 0);
    public GraphInfo GraphInfo { get; set; }

    /// <summary> bit 간 순방향 의존성 map </summary>
    public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new();
    /// <summary> bit 간 역방향 의존성 map </summary>
    public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; internal set; }
    /// <summary> this Cpu 관련 tags.  Root segment 의 S/R/E 및 call 의 Tx, Rx </summary>
    public BitDic BitsMap { get; } = new();
    public TagDic TagsMap { get; } = new();
    /// <summary> Call 의 TX RX 에 사용된 tag 목록 </summary>
    public List<Tag> TxRxTags { get; } = new List<Tag>();

    public Cpu(string name, Model model) : base(name)
    {
        Model = model;
        model.Cpus.Add(this);
    }

}


public static class CpuExtension
{
    static ILog Logger => Global.Logger;
    public static void AddTag(this Cpu cpu, Tag tag)
    {
        Debug.Assert(tag.Cpu == cpu);
        if (cpu.BitsMap.ContainsKey(tag.Name))
        {
            Debug.Assert(cpu.BitsMap[tag.Name] == tag);
            return;
        }
        cpu.BitsMap.Add(tag.Name, tag);
    }
    public static IEnumerable<IBit> CollectBits(this Cpu cpu)
    {
        IEnumerable<IBit> helper()
        {
            foreach (var map in new[] { cpu.ForwardDependancyMap, cpu.BackwardDependancyMap })
            {
                if (map == null)
                    continue;

                foreach (var tpl in map)
                {
                    yield return tpl.Key;
                    foreach (var v in tpl.Value)
                        yield return v;
                }
            }
        }

        return helper().Distinct();
    }



    public static void PrintTags(this Cpu cpu)
    {
        var tagNames = string.Join("\r\n\t", cpu.BitsMap.Values.OfType<Tag>().Select(t => t.Name));
        Logger.Debug($"{cpu.Name} tags:\r\n\t{tagNames}");
    }

    public static void PrintAllTags(this Cpu cpu, bool expand)
    {
        IEnumerable<string> helper()
        {
            foreach (var bit in cpu.BitsMap.Values)
            {
                var type = bit.GetType().Name;
                var name = bit is Named ? $" {((Named)bit).Name}" : "";
                yield return $"[{type}]{name} = {bit.ToText(expand)}";
            }
        }
        helper()
            .OrderBy(x => x)
            .Iter(Logger.Debug)
            ;
    }



    //public static void PrintTags(this CpuBase cpu)
    //{
    //    var tags = cpu.Tags.ToArray();
    //    var externalTagNames = string.Join("\r\n\t", tags.Where(t => t.IsExternal()).Select(t => t.Name));
    //    var internalTagNames = string.Join("\r\n\t", tags.Where(t => !t.IsExternal()).Select(t => t.Name));
    //    Logger.Debug($"-- Tags for {cpu.Name}");
    //    Logger.Debug($"  External:\r\n\t{externalTagNames}");
    //    Logger.Debug($"  Internal:\r\n\t{internalTagNames}");
    //}
}


public static class CpuExtensionBitChange
{
    [Obsolete("Old version")]
    public static void AddBitDependancy(this Cpu cpu, IBit source, IBit target)
    {
        Debug.Assert(source is not null && target is not null);

        var fwdMap = cpu.ForwardDependancyMap;

        if (!fwdMap.ContainsKey(source))
        {
            var srcTag = source as Tag;
            if (srcTag != null)
            {
                var xxx = fwdMap.Keys.OfType<Tag>().FirstOrDefault(k => k.Name == srcTag.Name);
                Debug.Assert(!fwdMap.Keys.OfType<Tag>().Any(k => k.Name == srcTag.Name));
            }


            fwdMap[source] = new HashSet<IBit>();
        }

        fwdMap[source].Add(target);
    }

    public static void BuildTagsMap(this Cpu cpu)
    {
        cpu.BitsMap
            .Where(kv => kv.Value is Tag && !cpu.TagsMap.ContainsKey(kv.Key))
            .Iter(kv => cpu.TagsMap.Add(kv.Key, kv.Value as Tag))
            ;
    }

    [Obsolete("Old version")]
    public static void BuildBackwardDependency(this Cpu cpu)
    {
        cpu.BackwardDependancyMap = new Dictionary<IBit, HashSet<IBit>>();
        var bwdMap = cpu.BackwardDependancyMap;

        foreach (var tpl in cpu.ForwardDependancyMap)
        {
            (var source, var targets) = (tpl.Key, tpl.Value);

            foreach (var t in targets)
            {
                if (!bwdMap.ContainsKey(t))
                    bwdMap[t] = new HashSet<IBit>();

                bwdMap[t].Add(source);
            }
        }
    }
}








/// <summary>
/// Queue 이용한 구현
/// </summary>
public static class CpuExtensionQueueing
{
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
            : base(bc.Bit, bc.NewValue, bc.Applied, bc.Cause)
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

    public static IDisposable Run(this Cpu cpu)
    {
        var disposable = new CancellationDisposable();
        var q = cpu.Queue;
        var fwd = cpu.ForwardDependancyMap;
        var indent = 0;
        var flipFlops = cpu.BitsMap.Values.OfType<FlipFlop>().ToArray();


        var ffSetterMap = flipFlops.GroupByToDictionary(ff => ff.S);
        var ffResetterMap = flipFlops.GroupByToDictionary(ff => ff.R);

        new Thread(async () =>
        {
            while (!disposable.IsDisposed && cpu.Running)
            {
                while (q.Count > 0)
                {
                    cpu.ProcessingQueue = true;
                    if (q.TryDequeue(out BitChange bitChange))
                    {
                        Debug.Assert(!bitChange.Applied);
                        //Global.Logger.Debug($"= Processing bitChnage {bitChange}");
                        Apply(cpu, bitChange, true);
                    }
                    else
                        Global.Logger.Warn($"Failed to deque.");
                }
                cpu.ProcessingQueue = false;
                await Task.Delay(20);
            }
        }).Start()
        ;

        _applyDirectly = new Action<Cpu, BitChange>((cpu, bitChange) => Apply(cpu, bitChange, false));

        return disposable;



        void Apply(Cpu cpu, BitChange bitChange, bool withQueue)
        {
            if (bitChange.Bit.GetName() == "ResetLatch_VPS_B")
                Console.WriteLine();

            indent++;
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
                            select (new BitChange(dep, newValue, false, bit))
                        ).ToList();

                    if (ffSetterMap.ContainsKey(bit) && bitChange.NewValue)
                        foreach(var ff in ffSetterMap[bit].Where(ff => ! ff.Value))
                            changes.Add(new BitChange(ff, true, false, bit));

                    if (ffResetterMap.ContainsKey(bit) && bitChange.NewValue)
                        foreach (var ff in ffResetterMap[bit].Where(ff => ff.Value))
                            changes.Add(new BitChange(ff, false, false, bit));

                    var chgrp = changes.GroupByToDictionary(ch => ch.Bit is PortInfo);
                    if (chgrp.ContainsKey(false))
                    {
                        var nonPorts = chgrp[false];
                        foreach (var bc in nonPorts)
                            Apply(cpu, bc, withQueue);
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
                                    Apply(cpu, newBc, withQueue);
                            }
                            else if (bit == port.Actual)
                            {
                                var newBc = new PortInfoActualChange(bc);
                                if (withQueue)
                                    q.Enqueue(newBc);
                                else
                                    Apply(cpu, newBc, withQueue);

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
            indent--;


            void DoApply(BitChange bitChange)
            {
                Debug.Assert(!bitChange.Applied);
                var bit = (Bit)bitChange.Bit;
                //Global.Logger.Debug($"\t=({indent}) Applying bitchange {bitChange}");

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

                bitChange.Applied = true;

                if (bitChanged)
                    Global.RawBitChangedSubject.OnNext(bitChange);
            }

        }
    }


    /// <summary> Bit 의 값 변경 처리를 CPU 에 위임.  즉시 수행되지 않고, CPU 의 Queue 에 추가 된 후, CPU thread 에서 수행된다.  </summary>
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue, object cause)
    {
        switch(bit)
        {
            case Expression _:
            case BitReEvaluatable re when re is not PortInfo:
                throw new Exception("ERROR: Expression can't be set!");
            default:
                cpu.Queue.Enqueue(new BitChange(bit, newValue, false, cause));
                break;
        };
    }
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue) => Enqueue(cpu, bit, newValue, null);


    static Action<Cpu, BitChange> _applyDirectly = null;
    public static void SendChange(this Cpu cpu, IBit bit, bool newValue, object cause) => _applyDirectly(cpu, new BitChange(bit, newValue, false, cause));
    public static void PostChange(this Cpu cpu, IBit bit, bool newValue, object cause) => Enqueue(cpu, bit, newValue, cause);
}
