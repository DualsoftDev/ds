using DsWebApp.Shared;

using Engine.Core;
using Engine.Runtime;
using Microsoft.AspNetCore.Mvc;

using static Engine.Core.TagKindModule;

namespace DsWebApp.Server.Controllers;

/// <summary>
/// DS Model controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBaseWithLogger
{
    ServerGlobal _global;
    RuntimeModel _model;
    public ModelController(ServerGlobal global)
        : base(global.Logger)
    {
        _global = global;
        _model = global.RuntimeModel;
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

    [HttpGet("tag")]
    public TagWeb[] GetAllHmiTags()
    {
        return _model?.Cpu?.GetWebTags().ToArray() ?? Array.Empty<TagWeb>();
    }

    [HttpGet("tag/{fqdn}/get")]
    public TagWeb GetHmiTag(string fqdn)
    {
        return _model?.Cpu?.GetWebTags().FirstOrDefault(wt => wt.Name == fqdn);
    }

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
        return new RuntimeModelDto
        {
            SourceDsZipPath = model.SourceDsZipPath,
            IsCpuRunning = model.Cpu?.IsRunning ?? false,
        };
    }
}
