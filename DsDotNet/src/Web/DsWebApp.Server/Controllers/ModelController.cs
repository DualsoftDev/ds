using DsWebApp.Server.Common;
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
}

