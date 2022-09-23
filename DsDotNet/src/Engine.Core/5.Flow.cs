
using System.Security.Policy;
using static Engine.Core.DsType;

namespace Engine.Base;

public abstract class Flow : Named, IWallet
{
    public virtual Cpu Cpu { get; }

    /// <summary>Edge 를 통해 알 수 없는 isolated segement/call 등을 포함 </summary>
    HashSet<IVertex> _childVertices = new();
    /// <summary> {RootCall, Segment, Child} instances </summary>
    public IEnumerable<IVertex> ChildVertices => _childVertices;
    //public void AddChildVertices(IEnumerable<IVertex> children)// 임시
    //{
    //    foreach (var child in children)
    //        AddChildVertex(child);
    //}
    public void AddChildVertex(IVertex child)
    {
        Assert(this is RootFlow || child is Child);
        Assert(!(child is CallPrototype));
        _childVertices.Add(child);
    }

    public GraphInfo GraphInfo { get; set; }

    List<Edge> _edges = new();
    public IEnumerable<Edge> Edges => _edges;

    public IEnumerable<ICoin> Coins => ChildVertices.OfType<ICoin>();
    public IEnumerable<ICoin> IsolatedCoins => this.CollectIsolatedCoins();

    public void AddEdge(Edge edge)
    {
        this.CheckAddable(edge);
        _edges.Add(edge);

        //edge.Sources.Iter(s => ChildVertices.Add(s));
        edge.Sources.Iter(s => AddChildVertex(s));
        //ChildVertices.Add(edge.Target);
        AddChildVertex(edge.Target);
    }

    protected Flow(Cpu cpu, string name)
        : base(name)
    {
        Cpu = cpu;
    }

    public Dictionary<string, object> InstanceMap = new();
}



[DebuggerDisplay("[{ToText()}]")]
public class RootFlow : Flow
{
    public DsSystem System { get; set; }
    public List<CallPrototype> CallPrototypes = new();

    public string[] NameComponents => new[] { System.Name, Name };
    public string QualifiedName => NameComponents.Combine();
    public RootFlow(Cpu cpu, string name, DsSystem system)
        : base(cpu, name)
    {
        System = system;
        system.RootFlows.Add(this);
        Auto = new TagE(cpu, null, $"Auto_{name}_{EmLinq.UniqueId()}", TagType.Auto);
    }

    public TagE Auto { get; }

    public IEnumerable<SegmentBase> RootSegments => ChildVertices.OfType<SegmentBase>();

    public override string ToText() => $"{QualifiedName}, #seg={RootSegments.Count()}, #chilren={ChildVertices.Count()}, #edges={Edges.Count()}";

    // alias : ppt 도형으로 modeling 하면 문제가 되지 않으나, text grammar 로 서술할 경우, 
    // 동일 이름의 call 등이 중복 사용되면, line 을 나누어서 기술할 때, unique 하게 결정할 수 없어서 도입.
    // e.g Ap = { Ap1; Ap2;}
    /// <summary> mnemonic -> target : "Ap1" -> "My.F.Ap", "My.F.Ap2" -> "My.F.Ap" </summary>
    public Dictionary<string, string[]> AliasNameMaps = new();
    /// <summary>target -> mnemonics : "My.F.Ap" -> ["Ap1"; "Ap2"] </summary>
    public Dictionary<string[], string[]> BackwardAliasMaps = new(NameComponentsComparer.Instance);

}

public class ChildFlow : Flow
{
    public ChildFlow(Cpu cpu, string name)
        : base(cpu, name)
    {
    }

    public IEnumerable<Child> Children => ChildVertices.OfType<Child>();

    public override string ToText()
    {
        return Name;
    }
}

public class NameComponentsComparer : IEqualityComparer<string[]>
{
    public bool Equals(string[] x, string[] y) => x.Length == y.Length && x.SequenceEqual(y);

    public int GetHashCode(string[] obj) => (int)obj.Average(ob => ob.GetHashCode());
    public static NameComponentsComparer Instance { get; } = new NameComponentsComparer();
}

public static class FlowExtension
{
    public static DsSystem GetSystem(this Flow flow)
    {
        switch (flow)
        {
            case RootFlow rf: return rf.System;
            case SegmentBase seg: return seg.ContainerFlow.System;
            default:
                throw new Exception("ERROR");
        }
    }


    public static IEnumerable<IVertex> CollectIsolatedVertex(this Flow flow, bool bySetEdge, bool byResetEdge)
    {
        var verticesFromEdge =
            flow.Edges
            .Where(e => bySetEdge == e is ISetEdge || byResetEdge == e is IResetEdge)
            .SelectMany(e => e.Vertices)
            ;
        return flow.ChildVertices
            .Except(verticesFromEdge)
            ;
    }
    public static IEnumerable<ICoin> CollectIsolatedCoins(this Flow flow, bool bySetEdge = true, bool byResetEdge = false) =>
        flow.CollectIsolatedVertex(bySetEdge, byResetEdge).OfType<ICoin>();

    public class Causal
    {
        public IVertex Source;
        public IVertex Target;
        public bool IsReset => EdgeCausal.IsReset;
        public EdgeCausal EdgeCausal;

        public Causal(IVertex source, IVertex target, EdgeCausal edgeCausal)
        {
            Source = source;
            Target = target;
            EdgeCausal = edgeCausal;
        }

        public override string ToString()
        {
            return $"{Source} {EdgeCausal.ToText()} {Target}";
        }
    }

    public static IEnumerable<Causal> CollectArrow(this Edge edge)
    {
        var e = edge;
        foreach (var s in e.Sources)
            yield return new Causal(s, e.Target, EdgeCausalType(e.Operator))
                ;
    }

    public static IEnumerable<Causal> CollectArrow(this Flow flow)
    {
        foreach (var e in flow.Edges)
            foreach (var c in e.CollectArrow())
                yield return c;
        ;
    }

    /// <summary>
    /// 중복 정의 check
    /// e.g "A, B > C; A > C"
    /// </summary>
    internal static void CheckAddable(this Flow flow, Edge edge)
    {
        var duplicate = flow.CollectArrow().Intersect(edge.CollectArrow()).ToArray();
        if (duplicate.Any())
            throw new Exception($"ERROR: duplicated causals: {duplicate[0]}");
    }

    public static void PrintFlow(this Flow flow)
    {
        var active = flow.Cpu.IsActive ? "Active " : "";
        LogDebug($"== {active}Flow {flow.GetSystem().Name}::{flow.Name}");
        foreach (var v in flow.ChildVertices)
            LogDebug(v.ToString());
    }
}
