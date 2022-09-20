namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract partial class Edge : IEdge
{
    public Flow ContainerFlow;

    /// <summary> Conjuction : 사용자가 모델링한 source vertices</summary>
    public IVertex[] Sources { get; internal set; }

    /// <summary>사용자가 모델링한 target vertex</summary>
    public IVertex Target { get; internal set; }

    public string Operator;

    public Edge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
    {
        Assert(sources.All(s => s != null));
        Assert(target != null);
        Assert(sources.Append(target).All(n => n is Child || n is SegmentBase || n is RootCall || n is SubCall));

        ContainerFlow = containerFlow;
        Sources = sources;
        Target = target;
        Operator = operator_;
    }

}


public partial class Edge
{
    public List<Tag> SourceTags { get; } = new();
    public Tag TargetTag { get; internal set; }


    public IEnumerable<IVertex> Vertices => Sources.Append(Target);

    public bool IsSourcesTrue => SourceTags.All(t => t.Value);
    public virtual bool Value { get; set; }
    public virtual bool Evaluate() => Value;

    public Cpu Cpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }

    public bool IsRootEdge => ContainerFlow is RootFlow;
    public override string ToString() => ToText();
    public string ToText()
    {
        var ss = string.Join(", ", Sources.Select(s => s.GetQualifiedName()));
        return $"{ss} {Operator} {Target.GetQualifiedName()}[{this.GetType().Name}]";
    }
}


/// '>' or '>>'
public abstract class SetEdge : Edge, ISetEdge
{
    public SetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        : base(containerFlow, sources, operator_, target)
    { }
}
public abstract class ResetEdge : Edge, IResetEdge
{
    public ResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        : base(containerFlow, sources, operator_, target)
    { }
}
public class WeakSetEdge : SetEdge, IWeakEdge
{
    public WeakSetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        : base(containerFlow, sources, operator_, target)
    { }
}
public class StrongSetEdge : SetEdge, IStrongEdge
{
    public StrongSetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        : base(containerFlow, sources, operator_, target)
    { }
}
public class WeakResetEdge : ResetEdge, IWeakEdge
{
    public WeakResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        : base(containerFlow, sources, operator_, target)
    { }
}
public class StrongResetEdge : ResetEdge, IStrongEdge
{
    public StrongResetEdge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
        : base(containerFlow, sources, operator_, target)
    { }
}

public static class EdgeExtension
{
    public static string GetOperator(this Edge edge) =>
        edge switch
        {
            WeakSetEdge => ">",
            StrongSetEdge => ">>",
            WeakResetEdge => "|>",
            StrongResetEdge => "||>",
            _ => throw new NotImplementedException($"Edge type {edge.GetType().Name} not implemented."),
        };
}