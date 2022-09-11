using Engine.Base;

namespace Engine.Core;

public abstract class Flow : Named, IWallet
{
    public virtual Cpu Cpu { get; }

    /// <summary>Edge 를 통해 알 수 없는 isolated segement/call 등을 포함 </summary>
    HashSet<IVertex> _childVertices = new();
    /// <summary> {RootCall, Segment, Child} instances </summary>
    public IEnumerable<IVertex> ChildVertices => _childVertices;
    public void AddChildVertices(IEnumerable<IVertex> children)// 임시
    {
        foreach (var child in children)
            AddChildVertex(child);
    }
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
}



[DebuggerDisplay("[{ToText()}]")]
public class RootFlow : Flow
{
    public DsSystem System { get; set; }
    public string QualifiedName => $"{System.Name}_{Name}";
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
}

public class ChildFlow : Flow
{
    public ChildFlow(Cpu cpu, string name)
        : base(cpu, name)
    {
    }

    public IEnumerable<Child> Children => ChildVertices.OfType<Child>();
}


public static class FlowExtension
{
    public static DsSystem GetSystem(this Flow flow)
    {
        switch(flow)
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
    public static IEnumerable<ICoin> CollectIsolatedCoins(this Flow flow, bool bySetEdge=true, bool byResetEdge=false) =>
        flow.CollectIsolatedVertex(bySetEdge, byResetEdge).OfType<ICoin>();

    public class Causal
    {
        public IVertex Source;
        public IVertex Target;
        public bool IsReset => EdgeCausal.IsReset;
        public DsType.EdgeCausal EdgeCausal;
        
        public Causal(IVertex source, IVertex target, DsType.EdgeCausal edgeCausal)
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
            yield return new Causal(s, e.Target, DsType.EdgeCausalType(e.Operator))
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
