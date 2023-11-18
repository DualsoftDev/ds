using DsWebApp.Server.Hubs;
using DsWebApp.Shared;
using Engine.Runtime;

using static Engine.Core.TagWebModule;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// DS Model controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBaseWithLogger
{
    RuntimeModel _model;
    IHubContext<ModelHub> _hubContextModel;
    IHubContext<HmiTagHub> _hubContextHmiTag;
    public ModelController(ServerGlobal global, IHubContext<ModelHub> hubContextModel, IHubContext<HmiTagHub> hubContextHmiTag)
        : base(global.Logger)
    {
        global.Logger.Debug("ModelController 생성자 호출 됨");

        _model = global.RuntimeModel;
        _hubContextModel = hubContextModel;
        _hubContextHmiTag = hubContextHmiTag;

        //global.RuntimeModel?.Cpu.TagWebChangedSubject.Subscribe(tagWeb =>
        //{
        //    global.Logger.Debug("Server: Notifying TagWeb change to all clients");

        //    // "hub/hmitag"
        //    hubContextModel.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
        //});
    }

    /*
       {
           "sourceDsZipPath": "C:\\ProgramData\\Dualsoft\\DsModel\\exportDS.Zip",
           "isCpuRunning": false
       }
    */
    [HttpGet]
    //public RuntimeModelDto GetModelInfo() => _model?.ToDto();
    public ActionResult<RuntimeModelDto> GetModelInfo()
    {
        if (_model == null)
            return NotFound(); // 404 Not Found 반환

        return _model.ToDto();
    }

    /// <summary>
    /// "api/model/tag" : 모든 HMI 태그 정보를 반환
    /// </summary>
    [HttpGet("tag")]
    public HmiTagPackage GetAllHmiTags()
    {
        return _model?.HMITagPackage;
    }

    //[HttpGet("tag/{fqdn}/get")]
    //public TagWeb GetHmiTag(string fqdn)
    //{
    //    return _model?.Cpu?.GetWebTags().FirstOrDefault(wt => wt.Name == fqdn);
    //    //return _model?.HMITagPackage.FirstOrDefault(wt => wt.Name == fqdn);
    //}


    /// <summary>
    /// "api/model/tag : POST 로 지정된 HMI 태그 정보 update
    /// </summary>
    [HttpPost("tag")]
    public async Task<bool> SetHmiTag([FromBody] TagWeb tagWeb)
    {
        var cpu = _model?.Cpu;
        if (cpu == null)
            return false;

        await _hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
        //return cpu.UpdateTagWeb(tagWeb);
        return true;
    }
    /// <summary>
    /// "api/model/tag/{fqdn}/{tagKind}" : 지정된 HMI 태그 정보 update
    /// </summary>
    [HttpPost("tag/{fqdn}/{tagKind}")]
    public async Task<bool> SetHmiTag(string fqdn, int tagKind, [FromBody] string serializedObject)
    {
        var cpu = _model?.Cpu;
        if (cpu == null)
            return false;

        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var obj = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        // todo: implement
        //cpu.SetTag(fqdn, obj);
        await _hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, new TagWeb(fqdn, obj, tagKind));

        return true;
    }
}


public static class RuntimeModelDtoExtensions
{
    public static RuntimeModelDto ToDto(this RuntimeModel model)
    {
        bool isCpuRunning = model.Cpu?.IsRunning ?? false;
        return new RuntimeModelDto(model.SourceDsZipPath, isCpuRunning);
    }
}
