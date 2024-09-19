/*
 * WebServer 의 api/model/graph Api 에 의해서 호출된다.
 */

using Dual.Common.Core;
using Engine.Core;
using static Engine.Core.DsConstants;
using static Engine.Core.CoreModule;
using static Engine.Common.GraphModule;
using static DsWebApp.Shared.CyGraphEx;
using Dual.Common.Base.CS;

namespace DsWebApp.Shared;

/// <summary>
/// Cytoscape graph item
/// </summary>
public interface ICyItem
{
    string id { get; }
}

/// <summary>
/// FQDN 을 uniq 한 숫자 id 로 변환/관리
/// </summary>
public class FqdnIdManager
{
    Dictionary<string, string> _fqdn2IdDic = new();
    int _idCounter = 0;
    public string FetchId(string fqdn)
    {
        if (fqdn is null)
            return null;

        if (_fqdn2IdDic.TryGetValue(fqdn, out string id))
            return id;

        var newId = _idCounter++.ToString();
        _fqdn2IdDic.Add(fqdn, newId);
        return newId;
    }
}

public abstract class CyItem : ICyItem
{
    public string id { get; /*internal*/ set; }
    public string fqdn { get; /*internal*/ set; }
    public string content { get; /*internal*/ set; }
    protected CyItem() {}

    protected CyItem(FqdnIdManager idManager, string fqdn, string content)
    {
        this.fqdn = fqdn;
        this.id = idManager.FetchId(fqdn);
        this.content = content;
    }
    public abstract string Serialize();
    protected void Set(FqdnIdManager idManager, string fqdn, string content) =>
        (this.fqdn, this.id, this.content) = (fqdn, idManager.FetchId(fqdn), content);
}

public class CyVertex : CyItem
{
    public string parent { get; /*internal*/ set; }
    public string type { get; /*internal*/ set; }

    public CyVertex() {}

    //public CyVertex(Vertex vertex)
    //    : this(vertex.GetType().Name, vertex.QualifiedName, vertex.Name, vertex.Parent.GetCore().QualifiedName)
    //{
    //}
    public CyVertex(FqdnIdManager idManager, string type, string fqdn, string content, string parent)
        : base(idManager, fqdn, content)
    {
        this.parent = idManager.FetchId(parent);
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

    public CyEdge(FqdnIdManager idManager, Edge edge)
        : this( idManager, edge.Source.QualifiedName, edge.Target.QualifiedName)
    {
        var et = edge.EdgeType;
        if (!et.IsOneOf(EdgeType.Start, EdgeType.Reset))
            throw new Exception("Check.  Not Start or Reset edge.");

        type = et.ToString();
    }

    CyEdge(FqdnIdManager idManager, string src, string tgt)
        : base(idManager, $"{idManager.FetchId(src)}_{idManager.FetchId(tgt)}", $"{src}__{tgt}")
    {
        this.source = idManager.FetchId(src);
        this.target = idManager.FetchId(tgt);
    }

    public void Set(FqdnIdManager idManager, string fqdn, string content, string source, string target, string type)
    {
        this.source = source;
        this.target = target;
        this.type = type;
        base.Set(idManager, fqdn, content);
    }

    public override string Serialize()
    {
        var data = $"id: '{id}', source: '{source}', target: '{target}'";
        if (type.Contains(","))
            Console.Write("");
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

    /// <summary>
    /// Brace ('{', '}') 로 감싸기
    /// </summary>
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