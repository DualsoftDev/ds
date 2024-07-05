using Dual.Common.Core;
using Engine.Core;
using static Engine.Core.CoreModule;
using static DsWebApp.Shared.CytoGraphEx;

namespace DsWebApp.Shared;

/// <summary>
/// Cytoscape graph item
/// </summary>
public interface ICytoItem
{
    string id { get; }
}

public abstract class CytoItem : ICytoItem
{
    public string id { get; }
    public string content { get; }
    protected CytoItem() {}

    protected CytoItem(string id, string content)
    {
        this.id = id;
        this.content = content;
    }
    public abstract string Serialize();
}

public class CytoVertex : CytoItem
{
    public string parent;
    public string type;

    public string shape;

    public CytoVertex() {}

    public CytoVertex(Vertex vertex)
        : this(vertex.GetType().Name, vertex.QualifiedName, vertex.Name, vertex.Parent.GetCore().QualifiedName)
    {
    }
    public CytoVertex(string type, string fqdn, string content, string parent)
        : base(fqdn, content)
    {
        this.parent = parent;
        this.type = type;
        shape = type switch
        {
            "Call" => "ellipse",
            _ => "rectangle"

        };
    }
    public override string Serialize()
    {
        var p = parent.IsNullOrEmpty() ? "" : $", parent: '{parent}'";
        var posi = ", position: " + Embrace("x: 215, y: 85");
        return CytoGraphEx.Embrace($"id: '{id}', content: '{content}', type: '{type}', shape: '{shape}'{p}");
    }
}

public class CytoEdge : CytoItem
{
    public string source { get; }
    public string target { get; }
    public CytoEdge() { }

    public CytoEdge(Edge edge)
        : this(edge.Source.QualifiedName, edge.Target.QualifiedName)
    {}

    public CytoEdge(string source, string target)
        : base($"{source}=>{target}", $"{source}=>{target}")
    {
        this.source = source;
        this.target = target;
    }

    public override string Serialize()
    {
        return CytoGraphEx.Embrace($"id: '{id}', source: '{source}', target: '{target}'");
    }
}

public class CytoData
{
    public CytoItem data { get; }

    public CytoData(CytoItem item)
    {
        data = item;
    }
}

public class CytoGraph
{
    public CytoData[] nodes { get; }
    public CytoData[] edges { get; }

    public CytoGraph() {}

    public CytoGraph(IEnumerable<CytoVertex> nodes, IEnumerable<CytoEdge> edges)
    {
        this.nodes = nodes.Select(n => new CytoData(n)).ToArray();
        this.edges = edges.Select(e => new CytoData(e)).ToArray();
    }
}

public static class CytoGraphEx
{
    public static string OB = "{";
    public static string CB = "}";
    public static string Embrace(string s) => @$"{OB} {s} {CB}";
    public static string Serialize(this CytoData data)
    {
        return Embrace($"data: {data.data.Serialize()}");
    }
    public static string Serialize(this CytoGraph graph)
    {

        var nodes = string.Join(",\n", graph.nodes.Select(n => n.Serialize()));
        var edges = string.Join(",\n", graph.edges.Select(n => n.Serialize()));
        var items = $"nodes: [{nodes}], edges: [{edges}]";
        return Embrace(items);
    }
}