using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using WebHMI.Shared;
using WebHMI.Server.Hubs;

namespace WebHMI.Server.Controllers;

[ApiController]
[Route("api")]
public class DsUploaderController : ControllerBase
{
    readonly IHubContext<DsHub> hubContext;
    public DsUploaderController(IHubContext<DsHub> _hubContext)
    {
        hubContext = _hubContext;
    }

    [HttpPost("upload")]
    public IActionResult Upload(byte[] model)
    {
        if (DsModelLoader.storeModel(model))
            return Ok(new { success = true, message = "" });
        else
            return BadRequest(new { success = false, message = "failed to saving ds model" });
    }

    [HttpPut("set/{tag}")]
    public object Set(string tag, object value)
    {
        Trace.WriteLine($"Server got SET request {tag}={value}");
        hubContext.Clients.All.SendAsync("S2CSet", new Tuple<string, object>(tag, value));
        return 0;
    }

    [HttpGet]
    public IActionResult Tester()
    {
        return Ok(new { success = true, message = "http get succeed" });
    }
}