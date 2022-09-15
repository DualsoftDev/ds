namespace Engine.Core;

public class Model
{
    public List<DsSystem> Systems = new();
    public List<Cpu> Cpus { get; } = new();
    /// <summary> 가상 부모 목록.  debugging 용 </summary>
    public SegmentBase[] VPSs { get; set; }

  
}
public static class ModelExtension
{
    public static T FindObject<T>(this Model model, string qualifiedName) where T : class
    {
        var tokens = qualifiedName.Split(new[] { '.' });
        var n = tokens.Length;
        var sys = model.Systems.FirstOrDefault(s => s.Name == tokens[0]);
        if (n == 1 || sys == null)
            return sys as T;

        foreach(var rf in sys.RootFlows)
        {
            var cp = rf.CallPrototypes.FirstOrDefault(cp => cp.GetQualifiedName() == qualifiedName);
            if (cp != null)
                return cp as T;
        }

        var flow = sys.RootFlows.FirstOrDefault(f => f.Name == tokens[1]);
        if (flow != null)
        {
            if (n == 2)
                return flow as T;

            var unit =
                flow.ChildVertices.FirstOrDefault(v => v switch
                {
                    RootCall call => call.Name == tokens[2],
                    SegmentBase seg => seg.Name == tokens[2],
                    Child child => child.Name == tokens[2],
                    _ => throw new Exception("ERROR"),
                });

            if (n == 3)
                return unit as T;

            var grandsonName = String.Join(".", tokens.Skip(3));
            return unit switch
            {
                SegmentBase seg => seg.Children.FirstOrDefault(grandson => grandson.Name == grandsonName) as T,
                _ => throw new Exception("ERROR"),
            };
        }

        return null;
    }
}
