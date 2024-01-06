using Engine.Info;
using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.CoreModule;
using static Engine.Core.HmiPackageModule;
using static Engine.Core.InfoPackageModule;
using static Engine.Core.TagWebModule;
using static Engine.Cpu.RunTime;

using SimpleResult = Dual.Common.Core.ResultSerializable<string, string>;

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
    public SimpleResult GetInfoDashboard()
    {
        InfoSystem infoSystem = InfoPackageModuleExt.GetInfo(_model.System);
        // System.Text.Json.JsonSerializer.Serialize 는 동작 안함.
        string newtonJson = Newtonsoft.Json.JsonConvert.SerializeObject(infoSystem);
        return SimpleResult.Ok(newtonJson);
    }
}

