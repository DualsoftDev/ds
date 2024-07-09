using Engine.Runtime;
using Engine.Core;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;

namespace DsWebApp.Server.Controllers;

public class ModelControllerConstructor : ControllerBaseWithLogger
{
    public ModelControllerConstructor(ILog logger) : base(logger)
    {
        logger.Debug("ModelController 생성자 호출 됨");
    }
}

/// <summary>
/// DS Model controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelController(ServerGlobal serverGlobal) : ModelControllerConstructor(serverGlobal.Logger)
{
    RuntimeModel _model => serverGlobal.RuntimeModel;

    /*
       {
           "sourceDsZipPath": "C:\\ProgramData\\Dualsoft\\DsModel\\exportDS.Zip",
           "isCpuRunning": false
       }
    */
    // api/model
    [HttpGet]
    public async Task<RestResult<RuntimeModelDto>> GetModelInfo()
    {
        if (!await serverGlobal.StandbyUntilServerReadyAsync())
            return RestResult<RuntimeModelDto>.Err("Server not ready.");

        if (_model == null)
            return RestResult<RuntimeModelDto>.Err("No model"); // 404 Not Found 반환

        bool isCpuRunning = _model.Cpu?.IsRunning ?? false;
        var model = new RuntimeModelDto(_model.SourceDsZipPath, isCpuRunning);
        return RestResult<RuntimeModelDto>.Ok(model);
    }



    /// <summary>
    /// Get vertices
    /// </summary>
    // api/model/graph-vertices
    [HttpGet("graph-vertices")]
    public async Task<RestResultString> GetNodes()
    {
        if (!await serverGlobal.StandbyUntilServerReadyAsync())
            return RestResultString.Err("Server not ready.");

        var sys = _model.System;
        CyGraph.TheSystem = sys;
        var vertices = sys.CollectVertices().ToArray();
        CyGraph.TheSystem = null;

        var json = NewtonsoftJson.SerializeObject(vertices);
        var xxx = NewtonsoftJson.DeserializeObject<CyVertex[]>(json);
        return RestResultString.Ok(json);
    }

    /// <summary>
    /// Get graph info: nodes and edges
    /// </summary>
    // api/model/graph
    [HttpGet("graph")]
    public async Task<RestResultString> GetNodesAndEdges([FromQuery] string fqdn = null) // fqdn = "HelloDS.STN1.Work1"
    {
        if (!await serverGlobal.StandbyUntilServerReadyAsync())
            return RestResultString.Err("Server not ready.");

        var sys = _model.System;
        fqdn = fqdn ?? sys.Name;

        CyGraph.TheSystem = sys;

        IVertex node = null;
        var nameComponents = fqdn.SplitToFqdnComponents();
        if (fqdn == sys.Name)
            node = sys;
        else
        {
            if (fqdn.StartsWith($"{sys.Name}."))
                nameComponents = nameComponents.Skip(1).ToArray();
            node = _model.System.FindGraphVertex(nameComponents);
        }

        if (node == null)
            return RestResultString.Err($"Failed to find vertex with name: {fqdn}");

        var vertices = node.CollectVertices(true).ToArray();
        var edges = node.CollectEdges().ToArray();
        var multiEdgesGroups =
            edges.GroupBy(e => (e.source, e.target))
                .Where(g => g.Count() > 1)
                .ToArray()
            ;
        var multiEdges =
            multiEdgesGroups.Select(gr =>
            {
                var multiples = gr.ToArray();
                var classes = gr.Select(e => e.type).JoinString(", ");
                var edge = new CyEdge();
                var m0 = multiples[0];
                edge.Set(m0.id, m0.content, m0.source, m0.target, classes);
                return edge;
            }).ToArray();

        // edges 에서 중복된 edge 를 제거하고, 중복된 edge 를 표시하는 edge 를 추가한다.
        var finalEdges =
            edges
                .Where(e => !multiEdgesGroups.Any(gr => gr.Contains(e)))
                .Concat(multiEdges)
                .ToArray();

        var cytoGraph = new CyGraph(vertices, finalEdges);

        var json = cytoGraph.Serialize();
        Trace.WriteLine(json);

        CyGraph.TheSystem = null;

        return RestResultString.Ok(json);
    }
}


public static class CytoVertexExtension
{
    static (string, string, string) GetNameAndQualifiedNameAndParentName(IVertex vertex)
    {
        var n = (vertex as INamed).Name;
        var q = (vertex as IQualifiedNamed).QualifiedName;
        var p = vertex.GetParentName();
        return (q, n, p);
    }
    public static IEnumerable<CyVertex> CollectVertices(this IVertex vertex, bool includeMe = true)
    {
        if (includeMe)
        {
            var (q, n, p) = GetNameAndQualifiedNameAndParentName(vertex);
            var t = vertex.GetType().Name;

            // flow 별로 최 외곽에 배치 : top level flow
            if (vertex is Flow f && f.System == CyGraph.TheSystem)
                p = null;

            yield return new CyVertex(t, q, n, p);
        }
        switch (vertex)
        {
            case DsSystem s:
                foreach (var c in s.Flows.SelectMany(f => f.CollectVertices()))
                    yield return c;
                break;
            case Flow f:
                foreach (var c in f.Graph.Vertices.SelectMany(v => v.CollectVertices()))
                    yield return c;
                break;
            case Real r:
                foreach (var c in r.Graph.Vertices.SelectMany(v => v.CollectVertices()))
                    yield return c;
                break;
            case Call cc:
                //yield return c;
                break;
            default:
                yield break;
        }
    }

    public static IEnumerable<CyEdge> CollectEdges(this IVertex vertex)
    {
        switch (vertex)
        {
            case DsSystem s:
                foreach (var c in s.Flows.SelectMany(f => f.CollectEdges()))
                    yield return c;
                break;
            case Flow f:
                if (f.Name == "MES")
                {
                    var xxx = f.Graph.Edges.Select(e => new CyEdge(e)).ToArray();
                    Console.Write("");
                }
                foreach (var c in f.Graph.Edges.Select(e => new CyEdge(e)))
                    yield return c;

                foreach (var c in f.Graph.Vertices.SelectMany(v => v.CollectEdges()))
                    yield return c;
                break;
            case Real r:
                foreach (var c in r.Graph.Edges.Select(e => new CyEdge(e)))
                    yield return c;
                break;

            default:
                yield break;
        }
    }
}