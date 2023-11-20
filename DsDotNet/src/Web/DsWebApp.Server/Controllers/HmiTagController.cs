using DsWebApp.Server.Hubs;
using Engine.Runtime;
using static Engine.Core.TagWebModule;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// HmiTag controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HmiTagController(
        ServerGlobal global
        , IHubContext<HmiTagHub> hubContext
    ) : ControllerBaseWithLogger(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;

    /// <summary>
    /// "api/hmitag" : 모든 HMI 태그 정보를 반환
    /// </summary>
    [HttpGet]
    public HmiTagPackage GetAllHmiTags()
    {
        return _model?.HMITagPackage;
    }

    /// <summary>
    /// "api/hmitag : POST 로 지정된 HMI 태그 정보 update
    /// </summary>
    [HttpPost]
    public async Task<bool> SetHmiTag([FromBody] TagWeb tagWeb)
    {
        var cpu = _model?.Cpu;
        if (cpu == null)
            return false;

        ErrorMessage errMsg = cpu.UpdateTagWeb(tagWeb);
        await hubContext.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
        return errMsg.IsNullOrEmpty();
    }
    /// <summary>
    /// "api/hmitag/{fqdn}/{tagKind}" : 지정된 HMI 태그 정보 update
    /// </summary>
    [HttpPost("{fqdn}/{tagKind}")]
    public async Task<bool> SetHmiTag(string fqdn, int tagKind, [FromBody] string serializedObject)
    {
        var cpu = _model?.Cpu;
        if (cpu == null)
            return false;

        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var objHolder = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        var tagWeb = new TagWeb(fqdn, objHolder.RawValue, tagKind);
        ErrorMessage errMsg = cpu.UpdateTagWeb(tagWeb);
        await hubContext.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);

        return errMsg.IsNullOrEmpty();
    }
}

