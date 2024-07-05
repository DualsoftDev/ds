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

public abstract class CyItem : ICyItem
{
    public string id { get; }
    public string content { get; }
    protected CyItem() {}

    protected CyItem(string id, string content)
    {
        this.id = id;
        this.content = content;
    }
    public abstract string Serialize();
}

public class CyVertex : CyItem
{
    public string parent;
    public string type;

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
        var posi = ", position: " + Embrace("x: 215, y: 85");
        var data = $"id: '{id}', content: '{content}'{p}";
        data = $"data: {Embrace(data)}";
        var classes = $"classes: '{type}'";
        return Embrace($"{data}, {classes}");
    }
}

public class CyEdge : CyItem
{
    public string source { get; }
    public string target { get; }
    public CyEdge() { }

    public CyEdge(Edge edge)
        : this(edge.Source.QualifiedName, edge.Target.QualifiedName)
    {}

    public CyEdge(string source, string target)
        : base($"{source}=>{target}", $"{source}=>{target}")
    {
        this.source = source;
        this.target = target;
    }

    public override string Serialize()
    {
        var data = $"id: '{id}', source: '{source}', target: '{target}'";
        data = $"data: {Embrace(data)}";
        return Embrace(data);
    }
}

public class CyData
{
    public CyItem data { get; }

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