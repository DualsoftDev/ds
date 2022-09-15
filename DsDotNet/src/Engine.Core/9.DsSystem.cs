namespace Engine.Core;

public class DsSystem : Named
{
    public Model Model;
    public List<RootFlow> RootFlows = new();
    public DsSystem(string name, Model model)
        : base(name)
    {
        Model = model;
        model.Systems.Add(this);
    }
}

