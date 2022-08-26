using System.Collections.Concurrent;

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
    internal int DbgNestingLevel { get; set; }
    public int DbgThreadId { get; internal set; }

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

    public override string ToText() => $"Cpu [{Name}={DbgThreadId}]";
}


public static class CpuExtension
{
    static ILog Logger => Global.Logger;


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

