using DsWebApp.Server.Hubs;
using Engine.Runtime;

using Microsoft.AspNetCore.Authorization;

using static Engine.Core.HmiPackageModule;
using static Engine.Core.TagWebModule;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// HmiTag controller.  api/hmi
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Administrator")]
public class HmiController(
        ServerGlobal global
        , IHubContext<HmiTagHub> hubContext
    ) : ControllerBaseWithLogger(global.Logger)
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

    /// <summary>
    /// "api/hmi/tag : POST 로 지정된 HMI 태그 정보 update
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("tag")]
    public async Task<string> SetHmiTag([FromBody] TagWeb tagWeb)
    {
        await Console.Out.WriteLineAsync($"About to change {tagWeb.Name}={tagWeb.Value}");
        var cpu = _model?.Cpu;
        if (cpu == null)
            return "No Loaded Model";

        ErrorMessage errMsg = cpu.UpdateTagWeb(tagWeb);
        await hubContext.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
        return errMsg.IsNullOrEmpty() ? null : errMsg;
    }

    /// <summary>
    /// "api/hmi/tag/{fqdn}/{tagKind}" : 지정된 HMI 태그 정보 update
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [HttpPost("tag/{fqdn}/{tagKind}")]
    public async Task<string> SetHmiTag(string fqdn, int tagKind, [FromBody] string serializedObject)
    {
        var cpu = _model?.Cpu;
        if (cpu == null)
            return "No Loaded Model";

        var kindDescriptions = _model?.TagKindDescriptions;

        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var objHolder = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        var tagWeb = new TagWeb(fqdn, objHolder.RawValue, tagKind, kindDescriptions[tagKind]);
        ErrorMessage errMsg = cpu.UpdateTagWeb(tagWeb);
        await hubContext.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);

        return errMsg.IsNullOrEmpty() ? null : errMsg;
    }
}

