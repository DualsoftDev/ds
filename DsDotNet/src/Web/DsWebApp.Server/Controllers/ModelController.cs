using Engine.Runtime;
using Engine.Core;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
using Dual.Web.Blazor.Shared;

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
    /// Get graph info: gets nodes and edges
    /// return: string[2]: [graphJson, nodesJson]
    /// </summary>
    // api/model/graph
    [HttpGet("graph")]
    public async Task<RestResult<string[]>> GetNodesAndEdges([FromQuery] string fqdn = null) // fqdn = "HelloDS.STN1.Work1"
    {
        if (!await serverGlobal.StandbyUntilServerReadyAsync())
            return RestResult<string[]>.Err("Server not ready.");

        var sys = _model.System;
        fqdn = fqdn ?? sys.Name;

        CyGraph.TheSystem = sys;

        IVertex node = null;
        var nameComponents = fqdn.SplitToFqdn();
        if (fqdn == sys.Name)
            node = sys;
        else
        {
            if (fqdn.StartsWith($"{sys.Name}."))
                nameComponents = nameComponents.Skip(1).ToArray();
            node = _model.System.FindGraphVertex(nameComponents);
        }

        if (node == null)
            return RestResult<string[]>.Err($"Failed to find vertex with name: {fqdn}");

        FqdnIdManager idManager = new();


        var vertices = node.CollectVertices(idManager, true).ToArray();
        var edges = node.CollectEdges(idManager).ToArray();
        var multiEdgesGroups =
            edges.GroupBy(e => (e.source, e.target))
                .Where(g => g.Count() > 1)
                .ToArray()
            ;
        var multiEdges =
            multiEdgesGroups.Select(gr =>
            {
                var multiples = gr.ToArray();
                var classes = gr.Select(e => e.type).JoinString(" ");   // cytoscape 의 class 구분자는 ' '
                var edge = new CyEdge();
                var m0 = multiples[0];
                edge.Set(idManager, m0.fqdn, m0.content, m0.source, m0.target, classes);
                return edge;
            }).ToArray();

        // edges 에서 중복된 edge 를 제거하고, 중복된 edge 를 표시하는 edge 를 추가한다.
        var finalEdges =
            edges
                .Where(e => !multiEdgesGroups.Any(gr => gr.Contains(e)))
                .Concat(multiEdges)
                .ToArray();

        var cytoGraph = new CyGraph(vertices, finalEdges);

        var nodesJson = NewtonsoftJson.SerializeObject(vertices);
        var graphJson = cytoGraph.Serialize();
        Trace.WriteLine(graphJson);

        CyGraph.TheSystem = null;

        return RestResult<string[]>.Ok( new[] { graphJson, nodesJson } );
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
    public static IEnumerable<CyVertex> CollectVertices(this IVertex vertex, FqdnIdManager idManager, bool includeMe = true)
    {
        if (includeMe)
        {
            var (q, n, p) = GetNameAndQualifiedNameAndParentName(vertex);
            var t = vertex.GetType().Name;

            // flow 별로 최 외곽에 배치 : top level flow
            if (vertex is Flow f && f.System == CyGraph.TheSystem)
                p = null;

            yield return new CyVertex(idManager, t, q, n, p);
        }
        switch (vertex)
        {
            case DsSystem s:
                foreach (var c in s.Flows.SelectMany(f => f.CollectVertices(idManager)))
                    yield return c;

                //foreach (var t in s.TaskDevs)
                //    yield return new CyVertex(idManager, "TaskDev", t.QualifiedName, t.Name, null);

                break;

            case Flow f:
                foreach (var c in f.Graph.Vertices.SelectMany(v => v.CollectVertices(idManager)))
                    yield return c;
                break;

            case Real r:
                foreach (var c in r.Graph.Vertices.SelectMany(v => v.CollectVertices(idManager)))
                    yield return c;
                break;

            case Call cc:
                //yield return c;
                break;

            default:
                yield break;
        }
    }

    /* Flow 나 Real 의 ModelingEdges 를 사용할 경우, 현재 Group 정의된 edge(화살표 끝점없이 직선만으로 연결된 group) 정보 누락. e.g "A > { B, C } > D"
     */
    public static IEnumerable<CyEdge> CollectEdges(this IVertex vertex, FqdnIdManager idManager)
    {
        switch (vertex)
        {
            case DsSystem s:
                foreach (var c in s.Flows.SelectMany(f => f.CollectEdges(idManager)))
                    yield return c;
                break;

            case Flow f:
                foreach (var c in f.Graph.Edges.Select(e => new CyEdge(idManager, e)))
                    yield return c;

                foreach (var c in f.Graph.Vertices.SelectMany(v => v.CollectEdges(idManager)))
                    yield return c;
                break;

            case Real r:
                foreach (var c in r.Graph.Edges.Select(e => new CyEdge(idManager, e)))
                    yield return c;
                break;

            default:
                yield break;
        }
    }
}