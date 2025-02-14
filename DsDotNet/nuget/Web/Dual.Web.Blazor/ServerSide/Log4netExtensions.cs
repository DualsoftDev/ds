using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using Dual.Common.Core;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dual.Web.Blazor.ServerSide;

public static class Log4netExtensions
{
    public static ILog AddLog4net(this IServiceCollection services, string loggerName)
    {
        var logConfig = "log4net.config";
        XmlConfigurator.ConfigureAndWatch(new FileInfo(logConfig));      // ConfigureAndWatch 동작하지 않는 듯.
        var logger = LogManager.GetLogger(loggerName);
        logger.Info($"== Starting {loggerName}");
        services.AddSingleton(logger);
        services.AddLogging(builder => builder.AddLog4Net(logConfig));
        return logger;
    }

    public static void AddTraceLogAppender(this IServiceCollection services, string loggerName)
        => TraceLogAppender.Add(loggerName);
}
