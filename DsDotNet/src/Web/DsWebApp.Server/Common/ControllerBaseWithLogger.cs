using log4net;

using Microsoft.AspNetCore.Mvc;

namespace DsWebApp.Server.Common
{
    public abstract class ControllerBaseWithLogger : ControllerBase
    {
        protected ILog _logger;
        protected ControllerBaseWithLogger(ILog logger)
        {
            _logger = logger;
        }
    }
}
