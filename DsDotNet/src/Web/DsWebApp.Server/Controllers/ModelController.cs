using DsWebApp.Server.Hubs;
using Engine.Runtime;
using static Engine.Core.TagWebModule;

namespace DsWebApp.Server.Controllers;

public class ModelControllerConstructor : ControllerBaseWithLogger
{
    public ModelControllerConstructor(ILog logger) : base(logger)
    {
        logger.Debug("ModelController 생성자 호출 됨");
    }
}

/// <summary>
/// DS Model controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelController(
        ServerGlobal global
        , IHubContext<HmiTagHub> hubContextHmiTag
    ) : ModelControllerConstructor(global.Logger)
{
    RuntimeModel _model => global.RuntimeModel;

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

        ErrorMessage errMsg = cpu.UpdateTagWeb(tagWeb);
        await hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
        return errMsg.IsNullOrEmpty();
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
        var objHolder = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        var tagWeb = new TagWeb(fqdn, objHolder.RawValue, tagKind);
        ErrorMessage errMsg = cpu.UpdateTagWeb(tagWeb);
        await hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);

        return errMsg.IsNullOrEmpty();
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
