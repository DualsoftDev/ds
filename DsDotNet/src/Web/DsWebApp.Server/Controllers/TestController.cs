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
/// Test controller.  api/test
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class TestController(ServerGlobal serverGlobal) : ControllerBaseWithLogger(serverGlobal.Logger)
{
    RuntimeModel _model => serverGlobal.RuntimeModel;

    /// <summary>
    /// "api/test/common-app-settings"
    /// </summary>
    [HttpGet("common-app-settings")]
    public RestResultString GetCommonAppSettingsJson()
    {
        if (_model?.HMIPackage == null)
            return RestResultString.Err("No model.");

        return RestResultString.Ok(NewtonsoftJson.SerializeObject(serverGlobal.DsCommonAppSettings));
    }

    /// <summary>
    /// api/test/server-exception
    /// </summary>
    [HttpGet("server-exception")]
    public RestResultString GetServerException()
    {
        throw new Exception("테스트 서버 예외");
    }

}
