namespace Engine.Base;


public abstract class DsTask : Named
{
    public DsSystem System;
    public DsTask(string name, DsSystem system)
        : base(name)
    {
        System = system;
    }
    public List<CallPrototype> CallPrototypes = new();
    public string QualifiedName => $"{System.Name}_{Name}";
    public override string ToText() => $"{QualifiedName}[{this.GetType().Name}], #call proto={CallPrototypes.Count}";
}



/// <summary> System 하부에 정의된 task </summary>
[DebuggerDisplay("{ToText()}")]
public class SysTask : DsTask
{
    public SysTask(string name, DsSystem system)
        : base(name, system)
    {
        system.Tasks.Add(this);
    }
}


/// <summary> (System/) Flow 하부에 정의된 task </summary>
[DebuggerDisplay("{ToText()}")]
public class FlowTask : DsTask
{
    public FlowTask(RootFlow flow)
        : base("", flow.System)
    {
        flow.FlowTask = this;
    }
}

