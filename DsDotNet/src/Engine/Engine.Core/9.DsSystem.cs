namespace Engine.Core;

public class DsSystem : Named, IParserObject
{
    public Model Model;
    /// <summary> CPU host ip or domain name</summary>
    public string Ip { get; set; }
    public Cpu Cpu { get; }
    public List<RootFlow> RootFlows = new();
    public ButtonDic EmergencyButtons { get; } = new();
    public ButtonDic AutoButtons { get; } = new();
    public ButtonDic StartButtons { get; } = new();
    public ButtonDic ResetButtons { get; } = new();

    public DsSystem(string name, Model model)
        : base(name)
    {
        Cpu = new Cpu(name, this);
        Model = model;
        model.Systems.Add(this);
    }

    public string[] NameComponents => new[] {Name};
    public IEnumerable<IParserObject> SpitParserObjects()
    {
        yield return this;
        foreach (var rf in RootFlows)
        {
            foreach (var x in rf.SpitParserObjects())
                yield return x;
        }
    }
}

