using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.HmiPackageModule;
using static Engine.Core.TagWebModule;
using static Engine.Cpu.RunTime;

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
}

