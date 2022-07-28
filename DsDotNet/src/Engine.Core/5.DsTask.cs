namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public class DsTask : Named
{
    public DsSystem System;
    public List<CallPrototype> CallPrototypes = new();

    public DsTask(string name, DsSystem system)
        : base(name)
    {
        System = system;
        system.Tasks.Add(this);
    }

    public string QualifiedName => $"{System.Name}_{Name}";
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}], #call proto={CallPrototypes.Count}";
}

