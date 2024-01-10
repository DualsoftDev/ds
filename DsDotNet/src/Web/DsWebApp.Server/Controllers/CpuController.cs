using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using static Engine.Cpu.RunTime;
using ResultSS = Dual.Common.Core.ResultSerializable<string, string>;
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
    public ResultSS Run()
    {
        if (_cpu == null)
            return ResultSS.Err("No model loaded");
        if (_cpu.IsRunning)
            return ResultSS.Err("Already running");

        _cpu.RunInBackground();
        hubContextModel.Clients.All.SendAsync(SK.S2CNCpuRunningStatusChanged, true);
        return ResultSS.Ok("Ok");
    }

    // api/cpu/command/stop
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/stop")]
    public ResultSS Stop()
    {
        if (_cpu == null)
            return ResultSS.Err("No model loaded");
        if (! _cpu.IsRunning)
            return ResultSS.Err("Already stopped");
        _cpu.Stop();
        hubContextModel.Clients.All.SendAsync(SK.S2CNCpuRunningStatusChanged, false);
        return ResultSS.Ok("Ok");
    }

    // api/cpu/command/reload-model
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/reload-model")]
    public ResultSS ReloadModel()
    {
        global.ReloadRuntimeModel(global.ServerSettings);
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, global.ServerSettings.RuntimeModelDsZipPath);
        return ResultSS.Ok("Ok");
    }
}

