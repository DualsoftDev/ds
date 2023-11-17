using DsWebApp.Server.Hubs;
using DsWebApp.Shared;
using Engine.Runtime;

using Microsoft.AspNetCore.SignalR.Client;

using static Engine.Cpu.RunTime;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// DS Model controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CpuController(ServerGlobal global, IHubContext<ModelHub> hubContextModel) : ControllerBaseWithLogger(global.Logger)
{
    DsCPU _cpu => global.RuntimeModel?.Cpu;

    [HttpGet("isRunning")]
    public bool IsCpuRunning() => _cpu?.IsRunning ?? false;

    RuntimeModelDto modelDto(bool newIsCpuRunning) =>
        new RuntimeModelDto(global.ServerSettings.RuntimeModelDsZipPath, newIsCpuRunning);
    [HttpGet("command/run")]
    public ActionResult<ErrorMessage> Run()
    {
        if (_cpu == null)
            return "No model loaded";
        if (_cpu.IsRunning)
            return "Already running";

        _cpu.RunInBackground();
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(true));
        return "";
    }

    [HttpGet("command/stop")]
    public ActionResult<ErrorMessage> Stop()
    {
        if (_cpu == null)
            return "No model loaded";
        if (! _cpu.IsRunning)
            return "Already stopped";

        _cpu.Stop();
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(false));

        return "";
    }

}

