using DsWebApp.Server.Stream;
using Microsoft.AspNetCore.Mvc;

namespace DsWebApp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ScreenController : ControllerBase
{
    [HttpGet("screens")]
    public IActionResult GetScreens()
    {
        return Ok(Streaming.DsStream.DsLayout.GetServerChannels());
    }
    [HttpGet("viewmodes")]
    public IActionResult GetViewTypes()
    {
        return Ok(Streaming.DsStream.DsLayout.GetViewTypeList());
    }
}
