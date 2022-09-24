namespace Engine.Base;

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
        if (model.Systems.Any(sys => sys.Name == name))
            throw new Exception($"Duplicated system name [{name}].");

        model.Systems.Add(this);
    }

    public override string ToText()
    {
        return Name;
    }
}

