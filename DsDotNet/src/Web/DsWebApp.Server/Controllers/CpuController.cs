using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using static Engine.Cpu.RunTime;
using SimpleResult = Dual.Common.Core.ResultSerializable<string, string>;
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


    // api/cpu/command/run
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/run")]
    public SimpleResult Run()
    {
        if (_cpu == null)
            return SimpleResult.Err("No model loaded");
        if (_cpu.IsRunning)
            return SimpleResult.Err("Already running");

        _cpu.RunInBackground();
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(true));
        return SimpleResult.Ok("Ok");
    }

    // api/cpu/command/stop
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/stop")]
    public SimpleResult Stop()
    {
        if (_cpu == null)
            return SimpleResult.Err("No model loaded");
        if (! _cpu.IsRunning)
            return SimpleResult.Err("Already stopped");

        _cpu.Stop();
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(false));

        return SimpleResult.Ok("Ok");
    }
}

