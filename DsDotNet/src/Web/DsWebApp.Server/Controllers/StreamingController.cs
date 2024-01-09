

using Dual.Common.Core;
using Engine.Runtime;

using ResultSS = Dual.Common.Core.ResultSerializable<string, string>;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// Streaming controller.  GetScreens/GetViewTypes
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class StreamingController(ServerGlobal global) : ControllerBaseWithLogger(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;
    [HttpGet("screens")]
    public ResultSS GetScreens()
    {
        return ResultSS.Ok(_model.DsStreaming.DsLayout.GetServerChannels().JoinString(";"));
    }
    [HttpGet("viewmodes")]
    public ResultSS GetViewTypes()
    {
        return ResultSS.Ok(_model.DsStreaming.DsLayout.GetViewTypeList().JoinString(";"));
    }
}
