using Engine.Core;
using Microsoft.AspNetCore.Mvc;

namespace DsWebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HmiTagController : ControllerBaseWithLogger
{
    ServerGlobal _global;
    public HmiTagController(ServerGlobal global)
        : base(global.Logger)
    {
        _global = global;
    }

    [HttpGet]
    public TagWebModule.TagWeb[] GetAllHmiTags()
    {
        // todo: implement using dsCpu.GetWebTags()
        //return _global.RuntimeModel.Cpu.GetWebTags();

        return Array.Empty<TagWebModule.TagWeb>();
    }
}