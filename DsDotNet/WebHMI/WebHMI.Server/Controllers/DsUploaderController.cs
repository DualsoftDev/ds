using Microsoft.AspNetCore.Mvc;
using WebHMI.Shared;

namespace WebHMI.Server.Controllers;

[ApiController]
[Route("api")]
public class DsUploaderController : ControllerBase
{
    [HttpPost("upload")]
    public IActionResult Upload(byte[] model)
    {
        DsModelLoader.storeModel(model);
        return Ok(new { message = "ok", bytes = model });
    }
    [HttpGet]
    public IActionResult Tester()
    {
        return Ok(new { message = "ok" });
    }
}