using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using static Engine.Info.DBLoggerAnalysisDTOModule;
using static Engine.Info.DBLoggerORM;
using static Engine.Core.InfoPackageModule;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
using FlatSpans = System.Tuple<string, Engine.Info.DBLoggerAnalysisDTOModule.Span[]>[];
using static Engine.Core.ModelLoaderModule;
using static Engine.Info.LoggerDB;

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

    // api/info/logdb-base
    [HttpGet("logdb-base")]
    public async Task<RestResultString> GetLoggerDB()
    {
        using var conn = serverGlobal.CreateDbConnection();
        var modelId = 1;
        var logDB = await ORMDBSkeletonDTOExt.CreateAsync(modelId, conn, null);
        var logDBJson = logDB.Serialize();

        return RestResultString.Ok(logDBJson);
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
}
