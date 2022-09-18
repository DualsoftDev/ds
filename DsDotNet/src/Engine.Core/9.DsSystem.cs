namespace Engine.Core;

public class DsSystem : Named
{
    public Model Model;
    public List<RootFlow> RootFlows = new();
    public ButtonDic EmergencyButtons { get; } = new();
    public ButtonDic AutoButtons { get; } = new();
    public ButtonDic StartButtons { get; } = new();
    public ButtonDic ResetButtons { get; } = new();

    public DsSystem(string name, Model model)
        : base(name)
    {
        Model = model;
        if (model.Systems.Exists(sys => sys.Name == name))
            throw new Exception($"System name [{name}] duplicated.");

        model.Systems.Add(this);
    }
}

