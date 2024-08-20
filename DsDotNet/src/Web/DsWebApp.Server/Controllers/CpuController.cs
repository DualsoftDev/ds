using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using static Engine.Cpu.RunTimeModule;
using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;
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

    // api/cpu/command/run
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/run")]
    public RestResultString Run()
    {
        if (_cpu == null)
            return RestResultString.Err("No CPU instance to run");
        if (_cpu.IsRunning)
            return RestResultString.Err("CPU Already running");

        _cpu.RunInBackground();
        _logger.Info("CPU run in background.");

        hubContextModel.Clients.All.SendAsync(SK.S2CNCpuRunningStatusChanged, true);
        return RestResultString.Ok("Ok");
    }

    // api/cpu/command/stop
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/stop")]
    public RestResultString Stop()
    {
        if (_cpu == null)
            return RestResultString.Err("No running CPU for stop");
        if (! _cpu.IsRunning)
            return RestResultString.Err("CPU Already stopped");
        _cpu.Stop();
        _logger.Warn("Stopped CPU.");
        hubContextModel.Clients.All.SendAsync(SK.S2CNCpuRunningStatusChanged, false);
        return RestResultString.Ok("Ok");
    }

    // api/cpu/command/reload-model
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/reload-model")]
    public RestResultString ReloadModel()
    {
        var zipPath = global.ServerSettings.RuntimeModelDsZipPath;
        _logger.Info($"Reloading model: {zipPath}");
        global.ReloadRuntimeModel(global.ServerSettings);
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, zipPath);
        return RestResultString.Ok("Ok");
    }


    // api/cpu/command/set-runtime-package
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/set-runtime-package/{runtimePackage}")]
    public RestResultString SetRuntimePackage(RuntimePackageCs runtimePackage)
    {
        _logger.Info($"RuntimePackage changed: {global.ServerSettings.RuntimePackageCs} => {runtimePackage}");
        global.ServerSettings.RuntimePackageCs = runtimePackage;
        return RestResultString.Ok("Ok");
    }
}

