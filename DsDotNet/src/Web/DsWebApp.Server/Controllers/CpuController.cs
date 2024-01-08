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


    ResultSS isRunningOK(bool? currentRunningStatus)
    {
        if (_cpu == null)
            return ResultSS.Err("No model loaded");
        if (currentRunningStatus != null && currentRunningStatus != _cpu.IsRunning)
            return ResultSS.Err("Already running");

        return ResultSS.Ok("Ok");
    }

    // api/cpu/command/run
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/run")]
    public ResultSS Run() =>
        isRunningOK(currentRunningStatus:false).Match(
            ok => {
                _cpu.RunInBackground();
                hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(true));
                return ResultSS.Ok("Ok");
            },
            err => ResultSS.Err(err)
        );

    // api/cpu/command/stop
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/stop")]
    public ResultSS Stop() =>
        isRunningOK(currentRunningStatus: true).Match(
            ok => {
                _cpu.Stop();
                hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(false));
                return ResultSS.Ok("Ok");
            },
            err => ResultSS.Err(err)
        );

    // api/cpu/command/reload-model
    [Authorize(Roles = "Administrator")]
    [HttpGet("command/reload-model")]
    public ResultSS ReloadModel()
    {
        global.ReloadRuntimeModel(global.ServerSettings);
        hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, modelDto(false));
        return ResultSS.Ok("Ok");
    }
}

