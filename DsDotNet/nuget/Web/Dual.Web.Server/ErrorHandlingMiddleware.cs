using System.Net;
using System.Text.Json;

using log4net;

using Microsoft.AspNetCore.Diagnostics;

namespace Dual.Web.Server;

public class ErrorHandlingMiddleware(RequestDelegate next, ILog logger)
{
    public async Task Invoke(HttpContext context, IWebHostEnvironment env)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            await HandleExceptionAsync(context, env, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, IWebHostEnvironment env, Exception exception)
    {
        string result;
        var code = HttpStatusCode.InternalServerError;

        if (env.IsDevelopment())
        {
            var errorMessage = new
            {
                error = exception.Message,
                stack = exception.StackTrace,
                innerException = exception.InnerException
            };

            result = NewtonsoftJson.SerializeObject(errorMessage);
        }
        else
        {
            var errorMessage = new
            {
                error = exception.Message
            };

            result = NewtonsoftJson.SerializeObject(errorMessage);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}

public class ErrorDetails
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public override string ToString() => NewtonsoftJson.SerializeObject(this);
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILog logger)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    logger.Error($"Something went wrong: {contextFeature.Error}");
                    var details = new ErrorDetails
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = "Internal Server Error"
                    };
                    await context.Response.WriteAsync(details.ToString());
                }
            });
        });
    }
    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
