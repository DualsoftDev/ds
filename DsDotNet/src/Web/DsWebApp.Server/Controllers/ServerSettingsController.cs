using DsWebApp.Shared;

namespace DsWebApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerSettingsController(ServerGlobal global) : ControllerBaseWithLogger(global.Logger)
    {
        [HttpGet] public ServerSettings GetServerSettings() => global.ServerSettings;
    }
}