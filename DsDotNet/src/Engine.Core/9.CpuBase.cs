using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Disposables;

namespace Engine.Core;

public class Cpu : Named, ICpu
{
    public IEngine Engine { get; set; }
    public Model Model { get; }
    public bool IsActive { get; set; }
    /// <summary> this Cpu 가 관장하는 root flows </summary>
    public RootFlow[] RootFlows { get; }

    /// <summary> Bit change event queue </summary>
    public ConcurrentQueue<BitChange> Queue { get; } = new();
    public GraphInfo GraphInfo { get; set; }

    /// <summary> bit 간 순방향 의존성 map </summary>
    public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new();
    /// <summary> bit 간 역방향 의존성 map </summary>
    public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; internal set; }
    /// <summary> this Cpu 관련 tags.  Root segment 의 S/R/E 및 call 의 Tx, Rx </summary>
    public TagDic TagsMap { get; } = new();
    /// <summary> Call 의 TX RX 에 사용된 tag 목록 </summary>
    public List<Tag> TxRxTags { get; } = new List<Tag>();
    protected CompositeDisposable _disposables = new();

    public Cpu(string name, RootFlow[] rootFlows, Model model) : base(name)
    {
        RootFlows = rootFlows;
        Model = model;
        model.Cpus.Add(this);
        rootFlows.Iter(f => f.Cpu = this);
    }

    public void Epilogue()
    {
        var subs =
            Global.BitChangedSubject.Subscribe(bc =>
            {
                this.OnBitChanged(bc);
            });
        _disposables.Add(subs);
    }

}


public static class CpuExtension
{
    static ILog Logger => Global.Logger;
    public static void AddTag(this Cpu cpu, Tag tag) => cpu.TagsMap.Add(tag.Name, tag);
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
        var tagNames = String.Join("\r\n\t", cpu.TagsMap.Values.Select(t => t.Name));
        Logger.Debug($"{cpu.Name} tags:\r\n\t{tagNames}");
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
                Debug.Assert(!fwdMap.Keys.OfType<Tag>().Any(k => k.Name == srcTag.Name));


            fwdMap[source] = new HashSet<IBit>();
        }

        fwdMap[source].Add(target);
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

    /// <summary> 외부에서 tag 가 변경된 경우 </summary>
    public static void OnOpcTagChanged(this Cpu cpu, string tagName, bool value)
    {
        if (cpu.TagsMap.ContainsKey(tagName))
        {
            var tag = cpu.TagsMap[tagName];
            tag.Value = value;
        }
    }

    public static void OnBitChanged(this Cpu cpu, BitChange bitChange)
    {
        cpu.Queue.Enqueue(bitChange);
        cpu.ProcessQueue();
    }

    public static void ProcessQueue(this Cpu cpu)
    {
        BitChange bc;
        while (cpu.Queue.Count > 0)
        {
            while (cpu.Queue.TryDequeue(out bc))
            {
                var bit = bc.Bit;
                if (bc.NewValue != bit.Value)
                {
                    Debug.Assert(!bc.Applied);
                    bit.Value = bc.NewValue;
                }

                if (cpu.ForwardDependancyMap.ContainsKey(bit))
                {
                    foreach (var forward in cpu.ForwardDependancyMap[bit])
                        forward.Evaluate();
                }
            }

            Thread.Sleep(10);
        }
    }
}
