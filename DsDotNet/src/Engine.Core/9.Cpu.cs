using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.Core;

public class Cpu : Named, ICpu
{
    public IEngine Engine { get; set; }
    public Model Model { get; }
    public bool IsActive { get; set; }


    /// <summary> this Cpu 가 관장하는 root flows </summary>
    public List<RootFlow> RootFlows { get; } = new();

    /// <summary> Bit change event queue </summary>
    public ConcurrentQueue<BitChange> Queue { get; } = new();
    public bool ProcessingQueue { get; internal set; }
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
    public static void AddBitDependancy(this Cpu cpu, IBit source, IBit target)
    {
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


    public static void BuildBitDependencies(this Cpu cpu)
    {
        Debug.Assert(cpu.ForwardDependancyMap.IsNullOrEmpty());
        Debug.Assert(cpu.BackwardDependancyMap is null);

        cpu.BackwardDependancyMap = new();
        var grp = cpu.BitsMap.Values.GroupByToDictionary(b => b is BitReEvaluatable);
        var stems = grp[true].Cast<BitReEvaluatable>();
        var terminals = grp[false];

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

        foreach (var stem in stems)
            addSubRelationship(stem);

        var ffs = cpu.BitsMap.Values.OfType<FlipFlop>();
        foreach (var ff in ffs)
            addSubRelationship(ff);
    }

    public static IEnumerable<IBit> CollectForwardDependantBits(this Cpu cpu, IBit source)
    {
        var fwd = cpu.ForwardDependancyMap;
        if (!fwd.ContainsKey(source))
            yield break;

        foreach (var dep in cpu.ForwardDependancyMap[source])
        {
            yield return dep;
            foreach (var v in cpu.CollectForwardDependantBits(dep))
                yield return v;
        }
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
            while (!disposable.IsDisposed)
            {
                while (q.Count > 0)
                {
                    cpu.ProcessingQueue = true;
                    if (q.TryDequeue(out BitChange bitChange))
                    {
                        Debug.Assert(!bitChange.Applied);
                        //Global.Logger.Debug($"= Processing bitChnage {bitChange}");
                        Apply(bitChange);
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


        bool DoApply(BitChange bitChange)
        {
            Debug.Assert(!bitChange.Applied);
            var bit = (Bit)bitChange.Bit;

            //if (bit.Value == bitChange.NewValue)
            //{
            //    Global.Logger.Debug($"\t=({indent}) Skipping already same bitchange {bitChange}");
            //    return bit is BitReEvaluatable;
            //}
            //else
            {
                //Global.Logger.Debug($"\t=({indent}) Applying bitchange {bitChange}");
                bit.SetValueOnly(bitChange.NewValue);
                bitChange.Applied = true;
                Global.RawBitChangedSubject.OnNext(bitChange);
                return true;
            }
        }

        void Apply(BitChange bitChange)
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
                if (DoApply(bitChange))
                {
                    // 변경으로 인한 파생 변경 enqueue
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
                            Apply(bc);
                    }
                    if (chgrp.ContainsKey(true))
                    {
                        var ports = chgrp[true];
                        foreach (var bc in ports)
                            q.Enqueue(bc);
                    }
                }
            }
            else
            {
                //Global.Logger.Warn($"Failed to find dependency for {bit.GetName()}");
                DoApply(bitChange);
            }
            indent--;
        }

    }

    /// <summary> Bit 의 값 변경 처리를 CPU 에 위임.  즉시 수행되지 않고, CPU 의 Queue 에 추가 된 후, CPU thread 에서 수행된다.  </summary>
    public static void Enqueue(this Cpu cpu, IBit bit, bool newValue, object cause = null)
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

}
