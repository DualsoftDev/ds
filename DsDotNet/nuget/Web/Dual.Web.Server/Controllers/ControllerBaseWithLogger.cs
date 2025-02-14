using log4net;

using Microsoft.AspNetCore.Mvc;

namespace Dual.Web.Server.Controllers;

public abstract class ControllerBaseWithLogger : ControllerBase
{
    protected ILog _logger;
    protected ControllerBaseWithLogger(ILog logger)
    {
        _logger = logger;
    }
}
