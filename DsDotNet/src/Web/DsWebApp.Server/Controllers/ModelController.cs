using Engine.Core;
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
    public ModelController(ServerGlobal global)
        : base(global.Logger)
    {
        _global = global;
    }

    [HttpGet("tag")]
    public TagWeb[] GetAllHmiTags()
    {
        return _global.RuntimeModel?.Cpu?.GetWebTags().ToArray() ?? Array.Empty<TagWeb>();
    }

    [HttpGet("tag/{fqdn}/get")]
    public TagWeb GetHmiTag(string fqdn)
    {
        return _global.RuntimeModel?.Cpu?.GetWebTags().FirstOrDefault(wt => wt.Name == fqdn);
    }

    [HttpPost("tag/{fqdn}")]
    public bool SetHmiTag(string fqdn, [FromBody] string serializedObject)
    {
        // serializedObject : e.g "{\"RawValue\":false,\"Type\":1}"
        var cpu = _global.RuntimeModel?.Cpu;
        if (cpu == null)
            return false;

        var obj = Dual.Common.Core.FS.ObjectHolder.Deserialize(serializedObject);
        // todo: implement
        //cpu.SetTag(fqdn, obj);
        return true;
    }
}