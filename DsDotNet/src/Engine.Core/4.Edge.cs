namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Edge : IEdge
{
    public Flow ContainerFlow;

    /// <summary> Conjuction </summary>
    public IVertex[] Sources { get; internal set; }
    public IVertex Target { get; internal set; }
    public IEnumerable<IVertex> Vertices => Sources.Append(Target);

    public bool Value { get => Sources.All(v => v.Value); set => throw new NotImplementedException(); }
    public Cpu OwnerCpu { get => ContainerFlow.Cpu; set => throw new NotImplementedException(); }

    public string Operator;

    public Edge(Flow containerFlow, IVertex[] sources, string operator_, IVertex target)
    {
        Debug.Assert(sources.All(s => s != null));
        Debug.Assert(target != null);

        ContainerFlow = containerFlow;
        Sources = sources;
        Target = target;
        Operator = operator_;
    }
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

