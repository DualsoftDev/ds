using DsWebApp.Shared;
using Engine.Runtime;

using static Engine.CodeGenCPU.TagHMIModule;
using static Engine.Core.TagWebModule;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// DS Model controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelController(ServerGlobal global) : ControllerBaseWithLogger(global.Logger)
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

    [HttpPost("tag/{fqdn}")]
    public bool SetHmiTag(string fqdn, [FromBody] string serializedObject)
    {
        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var cpu = _model?.Cpu;
        if (cpu == null)
            return false;

        var obj = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        // todo: implement
        //cpu.SetTag(fqdn, obj);
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
