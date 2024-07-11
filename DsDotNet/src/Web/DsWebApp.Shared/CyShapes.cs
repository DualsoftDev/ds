using Dual.Common.Core;
using Engine.Core;
using static Engine.Core.CoreModule;
using static DsWebApp.Shared.CyGraphEx;

namespace DsWebApp.Shared;

/// <summary>
/// Cytoscape graph item
/// </summary>
public interface ICyItem
{
    string id { get; }
}

public static class FilterEx
{
    public static string FilterId(this string id)
    {
        List<string> invalidChars = "\"'!@#$%^&*()".ToEnumerable().ToList();
        foreach (char c in "\"'!@#$%^&*()")
            id = id.Replace(c, '_');
        return id;
    }
}
public abstract class CyItem : ICyItem
{
    public string id { get; /*internal*/ set; }
    public string fqdn { get; /*internal*/ set; }
    public string content { get; /*internal*/ set; }
    protected CyItem() {}

    protected CyItem(string fqdn, string content)
    {
        this.fqdn = fqdn;
        this.id = fqdn.FilterId();
        this.content = content;
    }
    public abstract string Serialize();
    protected void Set(string fqdn, string content) =>
        (this.fqdn, this.id, this.content) = (fqdn, fqdn.FilterId(), content);
}

public class CyVertex : CyItem
{
    public string parent { get; /*internal*/ set; }
    public string type { get; /*internal*/ set; }

    public CyVertex() {}

    public CyVertex(Vertex vertex)
        : this(vertex.GetType().Name, vertex.QualifiedName, vertex.Name, vertex.Parent.GetCore().QualifiedName)
    {
    }
    public CyVertex(string type, string fqdn, string content, string parent)
        : base(fqdn, content)
    {
        this.parent = parent;
        this.type = type;
    }
    public override string Serialize()
    {
        var p = parent.IsNullOrEmpty() ? "" : $", parent: '{parent}'";
        var data = $"id: '{id}', fqdn: '{fqdn}', content: '{content}'{p}";
        data = $"data: {Embrace(data)}";
        var classes = $"classes: '{type}'";
        return Embrace($"{data}, {classes}");
    }
}

public class CyEdge : CyItem
{
    public string source { get; /*internal*/ set; }
    public string target { get; /*internal*/ set; }
    public string type { get; /*internal*/ set; }
    public CyEdge() { }

    public CyEdge(Edge edge)
        : this(edge.Source.QualifiedName, edge.Target.QualifiedName)
    {
        type = edge.EdgeType.ToString();
    }

    CyEdge(string src, string tgt)
        : base($"{src}__{tgt}", $"{src}__{tgt}")
    {
        this.source = src.FilterId();
        this.target = tgt.FilterId();
    }

    public void Set(string fqdn, string content, string source, string target, string type)
    {
        this.source = source;
        this.target = target;
        this.type = type;
        base.Set(fqdn, content);
    }

    public override string Serialize()
    {
        var data = $"id: '{id}', source: '{source}', target: '{target}'";
        data = $"data: {Embrace(data)}, classes: '{type}'";
        return Embrace(data);
    }
}

public class CyData
{
    public CyItem data { get; /*internal*/ set; }

    public CyData(CyItem item)
    {
        data = item;
    }
}

public class CyGraph
{
    public static DsSystem TheSystem { get; set; }
    public CyData[] nodes { get; }
    public CyData[] edges { get; }

    public CyGraph() {}

    public CyGraph(IEnumerable<CyVertex> nodes, IEnumerable<CyEdge> edges)
    {
        this.nodes = nodes.Select(n => new CyData(n)).ToArray();
        this.edges = edges.Select(e => new CyData(e)).ToArray();
    }
}

public static class CyGraphEx
{
    public static string OB = "{";
    public static string CB = "}";
    public static string Embrace(string s) => @$"{OB} {s} {CB}";
    public static string Serialize(this CyData data) => data.data.Serialize();

    public static string Serialize(this CyGraph graph)
    {
        var nodes = string.Join(",\n", graph.nodes.Select(n => n.Serialize()));
        var edges = string.Join(",\n", graph.edges.Select(n => n.Serialize()));
        var items = $"nodes: [{nodes}], edges: [{edges}]";
        return Embrace(items);
    }
}