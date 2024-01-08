using Dual.Common.Core;

using Engine.Core;
using Engine.Info;
using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.CoreModule;
using static Engine.Core.HmiPackageModule;
using static Engine.Core.InfoPackageModule;
using static Engine.Core.TagWebModule;
using static Engine.Cpu.RunTime;

using ResultSS = Dual.Common.Core.ResultSerializable<string, string>;

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
    public ResultSerializable<InfoQueryResult, string> GetInfoQuery([FromQuery] string Fqdn, [FromQuery] DateTime Start, [FromQuery] DateTime End)
    {
        // todo: 검색 결과 생성
        return ResultSerializable<InfoQueryResult, string>.Ok(new InfoQueryResult());
        //return ResultSerializable<InfoQueryResult, string>.Err("Not implemented");
    }
}

