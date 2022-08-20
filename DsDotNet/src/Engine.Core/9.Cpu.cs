using System.Collections.Concurrent;
using System.Reactive.Joins;

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

    internal Dictionary<IBit, FlipFlop[]> FFSetterMap;
    internal Dictionary<IBit, FlipFlop[]> FFResetterMap;
    internal int NestingLevel { get; set; }

    public GraphInfo GraphInfo { get; set; }

    /// <summary> bit 간 순방향 의존성 map </summary>
    public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new();
    /// <summary> bit 간 역방향 의존성 map </summary>
    public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; internal set; }
    /// <summary> this Cpu 관련 tags.  Root segment 의 S/R/E 및 call 의 Tx, Rx </summary>
    public BitDic BitsMap { get; } = new();
    public TagDic TagsMap { get; } = new();

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
                if (type == "FlipFlop")
                    Console.WriteLine();
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

    public static void BuildFlipFlopMapOnDemand(this Cpu cpu)
    {
        if (cpu.FFSetterMap == null)
        {
            var flipFlops = cpu.BitsMap.Values.OfType<FlipFlop>().ToArray();

            cpu.FFSetterMap = flipFlops.GroupByToDictionary(ff => ff.S);
            cpu.FFResetterMap = flipFlops.GroupByToDictionary(ff => ff.R);
        }
    }
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
