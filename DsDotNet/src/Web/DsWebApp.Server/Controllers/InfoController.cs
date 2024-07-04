using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using static Engine.Info.DBLoggerAnalysisDTOModule;
using static Engine.Info.DBLoggerORM;
using static Engine.Core.InfoPackageModule;
using static Engine.Core.GraphModule;
using static Engine.Core.CoreModule;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
using FlatSpans = System.Tuple<string, Engine.Info.DBLoggerAnalysisDTOModule.Span[]>[];

namespace DsWebApp.Server.Controllers;

/// <summary>
/// Info controller.  api/info
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class InfoController(ServerGlobal global) : ControllerBaseWithLogger(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;

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
        using var conn = global.CreateDbConnection();
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

        using var conn = global.CreateDbConnection();
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

        using var conn = global.CreateDbConnection();
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
    public async Task<RestResultString> GetNodesAndEdges([FromQuery] string fqdn) // fqdn = "HelloDS.STN1.Work1"
    {
        var sys = _model.System;
        var nameComponents = fqdn.SplitToFqdnComponents();
        nameComponents = fqdn.StartsWith($"{sys.Name}.") ? nameComponents.Skip(1).ToArray() : nameComponents;
        var node = _model.System.FindGraphVertex(nameComponents);
        if (node == null)
            return null;

        Graph<Vertex, Edge> graph;
        CytoVertex me;
        switch (node)
        {
            case Flow f:
                graph = f.Graph;
                me = new CytoVertex(f.QualifiedName, f.Name, null);
                break;
            case Real r:
                graph = r.Graph;
                me = new CytoVertex(r.QualifiedName, r.Name, null);
                break;
            default:
                return null;
        }

        var vertices = graph.Vertices.Select(v => new CytoVertex(v)).Append(me);
        var edges = graph.Edges.Select(e => new CytoEdge(e));
        var cytoGraph = new CytoGraph(vertices, edges);

        var json = cytoGraph.Serialize();

        return RestResultString.Ok(json);
    }

}

