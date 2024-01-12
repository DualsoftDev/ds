using Dual.Common.Core;

using Engine.Core;
using Engine.Info;
using static Engine.Info.DBLoggerORM;
using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.CoreModule;
using static Engine.Core.HmiPackageModule;
using static Engine.Core.InfoPackageModule;
using static Engine.Core.TagWebModule;
using static Engine.Cpu.RunTime;

using ResultSS = Dual.Web.Blazor.Shared.RestResult<string>;

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
    public ResultSS GetInfoDashboard()
    {
        InfoSystem infoSystem = InfoPackageModuleExt.GetInfo(_model.System);
        // System.Text.Json.JsonSerializer.Serialize 는 동작 안함.
        string newtonJson = Newtonsoft.Json.JsonConvert.SerializeObject(infoSystem);
        return ResultSS.Ok(newtonJson);
    }

    // api/info/q
    [HttpGet("q")]
    public async Task<RestResult<InfoQueryResult>> GetInfoQuery([FromQuery] string Fqdn, [FromQuery] DateTime Start, [FromQuery] DateTime End)
    {
        using var conn = global.CreateDbConnection();
        ORMVwLog[] vwLogs =
            (await conn.QueryAsync<ORMVwLog>(
                $"SELECT * FROM [{Vn.Log}] WHERE [fqdn] = @Fqdn AND [at] BETWEEN @Start AND @End;",
                new { Fqdn, Start, End })).ToArray();

        // todo: 검색 결과 생성

        return RestResult<InfoQueryResult>.Ok(new InfoQueryResult());
    }
}

