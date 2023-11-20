using DsWebApp.Server.Hubs;
using Engine.Runtime;

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
public class ModelController(
        ServerGlobal global
        , IHubContext<HmiTagHub> hubContextHmiTag
    ) : ModelControllerConstructor(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;

    /*
       {
           "sourceDsZipPath": "C:\\ProgramData\\Dualsoft\\DsModel\\exportDS.Zip",
           "isCpuRunning": false
       }
    */
    [HttpGet]
    public ActionResult<RuntimeModelDto> GetModelInfo()
    {
        if (_model == null)
            return NotFound(); // 404 Not Found 반환

        bool isCpuRunning = _model.Cpu?.IsRunning ?? false;
        return new RuntimeModelDto(_model.SourceDsZipPath, isCpuRunning);
    }
}

