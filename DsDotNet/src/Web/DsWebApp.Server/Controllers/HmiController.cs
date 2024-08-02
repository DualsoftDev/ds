using Engine.Core;
using Engine.Info;
using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.HmiPackageModule;
using static Engine.Core.TagWebModule;
using static Engine.Cpu.RunTime;
using static Engine.Info.DBLoggerORM;

using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// HmiTag controller.  api/hmi
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class HmiController(ServerGlobal global) : ControllerBaseWithLogger(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;
    ILog logger => global.Logger;

    /// <summary>
    /// "api/hmi/package" : 모든 HMI 태그 정보를 반환
    /// </summary>
    [HttpGet("package")]
    public RestResultString GetHmiPackageRestString()
    {
        if (_model?.HMIPackage == null)
            return RestResultString.Err("No model.");

        return RestResultString.Ok(NewtonsoftJson.SerializeObject(_model?.HMIPackage));
    }
    async Task<RestResultString> onTagWebChangedByClientBrowserAsync(TagWeb tagWeb)
    {
        await Task.Yield();
        try
        {
            var cpu = _model?.Cpu;
            if (cpu == null)
                return RestResultString.Err("No Loaded Model");

            logger.Debug($"HmiTagHub has {HmiTagHub.ConnectedClients.Count} connections");
            _model.HMIPackage.UpdateTag(tagWeb);
            cpu.TagWebChangedFromWebSubject.OnNext(tagWeb);
            var storage = global.RuntimeModel.Storages[tagWeb.Name];
            DBLogger.EnqueLog(new DsLogModule.DsLog(DateTime.Now, storage, nullToken));

            //await hubContext.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);     <-- cpu.TagWebChangedSubject.OnNext 에서 수행 됨..
            return RestResultString.Ok("OK");
        }
        catch (Exception ex)
        {
            return RestResultString.Err(ex.Message);
        }
    }
    /// <summary>
    /// "api/hmi/tag : POST 로 지정된 HMI 태그 정보 update
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("tag")]
    public async Task<RestResultString> SetHmiTag([FromBody] TagWeb tagWeb)
    {
        await Console.Out.WriteLineAsync($"About to change {tagWeb.Name}={tagWeb.Value}");
        return await onTagWebChangedByClientBrowserAsync(tagWeb);
    }

    /// <summary>
    /// "api/hmi/tag/{fqdn}/{tagKind}" : 지정된 HMI 태그 정보 update
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("tag/{fqdn}/{tagKind}")]
    public async Task<RestResultString> SetHmiTag(string fqdn, int tagKind, [FromBody] string serializedObject)
    {
        if (_model == null)
            return RestResultString.Err("No Loaded Model");

        var kindDescriptions = _model.TagKindDescriptions;

        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var objHolder = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        var tagWeb = new TagWeb(fqdn, objHolder.RawValue, tagKind, kindDescriptions[tagKind]);

        return await onTagWebChangedByClientBrowserAsync(tagWeb);
    }
}

