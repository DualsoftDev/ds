using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

namespace Engine.Core;

public class Model
{
    public List<DsSystem> Systems = new();
    public List<Cpu> Cpus { get; } = new();

    internal Dictionary<string, Tag> _generatedTagsMap = new();
}
public static class ModelExtension
{
    public static void VerifyFirstCreateTag(this Cpu cpu, string tagName, Tag tag)
    {
        var map = cpu.Model._generatedTagsMap;
        var key = $"{cpu.Name}.{tagName}";

        Debug.Assert(!map.ContainsKey(key));
        map.Add(key, tag);
    }


    public static T FindObject<T>(this Model model, string qualifiedName) where T : class
    {
        var tokens = qualifiedName.Split(new[] { '.' });
        var n = tokens.Length;
        var sys = model.Systems.FirstOrDefault(s => s.Name == tokens[0]);
        if (n == 1 || sys == null)
            return sys as T;

        var task = sys.Tasks.FirstOrDefault(t => t.Name == tokens[1]);
        if (task != null)
        {
            if (n == 2)
                return task as T;
            var callProto = task.CallPrototypes.FirstOrDefault(c => c.Name == tokens[2]);
            Debug.Assert(n == 3);
            return callProto as T;
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
                    Segment seg => seg.Name == tokens[2],
                    Child child => child.Name == tokens[2],
                    _ => throw new Exception("ERROR"),
                });

            if (n == 3)
                return unit as T;

            var grandsonName = String.Join(".", tokens.Skip(3));
            return unit switch
            {
                Segment seg => seg.Children.FirstOrDefault(grandson => grandson.Name == grandsonName) as T,
                _ => throw new Exception("ERROR"),
            };
        }

        return null;
    }
}
