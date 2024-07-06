using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using static Engine.Info.DBLoggerAnalysisDTOModule;
using static Engine.Info.DBLoggerORM;
using static Engine.Core.InfoPackageModule;
using static Engine.Core.GraphModule;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
using FlatSpans = System.Tuple<string, Engine.Info.DBLoggerAnalysisDTOModule.Span[]>[];
using DsWebApp.Server.Common;
using DevExpress.Pdf.Native.BouncyCastle.Asn1.X509;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// Info controller.  api/info
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class InfoController(ServerGlobal serverGlobal) : ControllerBaseWithLogger(serverGlobal.Logger)
{
    RuntimeModel _model => serverGlobal.RuntimeModel;

    // api/info
    [HttpGet]
    public RestResultString GetInfoDashboard()
    {
        InfoSystem infoSystem = InfoPackageModuleExt.GetInfo(_model.System);
        // System.Text.Json.JsonSerializer.Serialize 는 동작 안함.
        string newtonJson = Newtonsoft.Json.JsonConvert.SerializeObject(infoSystem);
        return RestResultString.Ok(newtonJson);
    }

    // api/info/q
    [HttpGet("q")]
    public async Task<RestResult<InfoQueryResult>> GetInfoQuery([FromQuery] string fqdn, [FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        using var conn = serverGlobal.CreateDbConnection();
        ORMVwLog[] vwLogs =
            (await conn.QueryAsync<ORMVwLog>(
                $"SELECT * FROM [{Vn.Log}] WHERE [fqdn] = @fqdn AND [at] BETWEEN @start AND @end;",
                new { fqdn, start, end })).ToArray();

        // todo: 검색 결과 생성

        return RestResult<InfoQueryResult>.Ok(new InfoQueryResult());
    }

    // api/info/log-anal-info
    [HttpGet("log-anal-info")]
    public async Task<SystemSpan> GetLogAnalInfo([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        DateTime start1 = start ?? DateTime.MinValue;
        DateTime end1 = end ?? DateTime.MaxValue;

        using var conn = serverGlobal.CreateDbConnection();
        var logs =
            (await conn.QueryAsync<ORMVwLog>(
                $"SELECT * FROM [{Vn.Log}] WHERE [at] BETWEEN @start1 AND @end1;",
                new { start1, end1 })).ToArray();

        var sysSpan = SystemSpanEx.CreateSpan(_model.System, logs);

        return sysSpan;
    }
    // api/info/log-anal-info-flat
    [HttpGet("log-anal-info-flat")]
    public async Task<FlatSpans> GetLogAnalFlatInfo([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        DateTime start1 = start ?? DateTime.MinValue;
        DateTime end1 = end ?? DateTime.MaxValue;

        using var conn = serverGlobal.CreateDbConnection();
        var logs =
            (await conn.QueryAsync<ORMVwLog>(
                $"SELECT * FROM [{Vn.Log}] WHERE [at] BETWEEN @start1 AND @end1;",
                new { start1, end1 })).ToArray();

        var sysSpan = SystemSpanEx.CreateFlatSpan(_model.System, logs);

        return sysSpan;
    }

    /// <summary>
    /// Get graph info: nodes and edges
    /// </summary>
    // api/info/graph
    [HttpGet("graph")]
    public async Task<RestResultString> GetNodesAndEdges([FromQuery] string fqdn=null) // fqdn = "HelloDS.STN1.Work1"
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
        var multiEdgesGroups=
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
    public static IEnumerable<CyVertex> CollectVertices(this IVertex vertex, bool includeMe=true)
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