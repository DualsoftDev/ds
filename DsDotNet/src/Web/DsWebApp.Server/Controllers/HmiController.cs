using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.HmiPackageModule;
using static Engine.Core.TagWebModule;
using static Engine.Cpu.RunTime;

using SimpleResult = Dual.Common.Core.ResultSerializable<string, string>;

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

    /// <summary>
    /// "api/hmi/package" : 모든 HMI 태그 정보를 반환
    /// </summary>
    [HttpGet("package")]
    public ResultSerializable<HMIPackage, ErrorMessage> GetAllHmiTags()
    {
        return _model?.HMIPackage;
    }


    async Task<SimpleResult> onTagWebChangedByClientBrowserAsync(TagWeb tagWeb)
    {
        try
        {
            var cpu = _model?.Cpu;
            if (cpu == null)
                return SimpleResult.Err("No Loaded Model");

            await Console.Out.WriteLineAsync($"HmiTagHub has {HmiTagHub.ConnectedClients.Count} connections");
            _model.HMIPackage.UpdateTag(tagWeb);
            cpu.TagWebChangedFromWebSubject.OnNext(tagWeb);
            //await hubContext.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);     <-- cpu.TagWebChangedSubject.OnNext 에서 수행 됨..
            return SimpleResult.Ok("OK");
        }
        catch (Exception ex)
        {
            return SimpleResult.Err(ex.Message);
        }
    }
    /// <summary>
    /// "api/hmi/tag : POST 로 지정된 HMI 태그 정보 update
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("tag")]
    public async Task<SimpleResult> SetHmiTag([FromBody] TagWeb tagWeb)
    {
        await Console.Out.WriteLineAsync($"About to change {tagWeb.Name}={tagWeb.Value}");
        return await onTagWebChangedByClientBrowserAsync(tagWeb);
    }

    /// <summary>
    /// "api/hmi/tag/{fqdn}/{tagKind}" : 지정된 HMI 태그 정보 update
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("tag/{fqdn}/{tagKind}")]
    public async Task<SimpleResult> SetHmiTag(string fqdn, int tagKind, [FromBody] string serializedObject)
    {
        if (_model == null)
            return SimpleResult.Err("No Loaded Model");

        var kindDescriptions = _model.TagKindDescriptions;

        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var objHolder = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        var tagWeb = new TagWeb(fqdn, objHolder.RawValue, tagKind, kindDescriptions[tagKind]);

        return await onTagWebChangedByClientBrowserAsync(tagWeb);
    }
}

