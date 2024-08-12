using IO.WebServer.Data;
using Microsoft.Extensions.Hosting.WindowsServices;
using Dual.Web.Blazor.ServerSide;
using Dual.Common.Core;
//using Newtonsoft.Json;
using log4net;
using IO.WebServer.Demons;

using System.Diagnostics;
using Log4NetLogger = Dual.Common.Core.Log4NetLogger;
using Dual.Common.Base.CS;

bool isWinService = WindowsServiceHelpers.IsWindowsService();
PresetAppSettings(isWinService);

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    ContentRootPath = isWinService ? AppContext.BaseDirectory : default,
    Args = args
});

ConfigurationManager conf = builder.Configuration;

builder.Host.UseWindowsService();
string asService = isWinService ? " as a window service" : "";


IServiceCollection services = builder.Services;
ILog logger = services.AddLog4net("IOWebServerLogger");
DcLogger.Logger = logger;
services.AddTraceLogAppender("IOWebServerLogger");
logger.Info($"======================= IOWebServer started.");
logger.Info($"Debugger.IsAttached = {Debugger.IsAttached}");

// Add services to the container.
services.AddSignalR();
conf.AddEnvironmentVariables();

var urls = conf["ASPNETCORE_URLS"];
logger.Info($"ASPNETCORE_URLS = {urls}");



if (!isWinService)
{
    builder.Host.UseContentRoot(Directory.GetCurrentDirectory());
    Debug.WriteLine($"Current = {Directory.GetCurrentDirectory()}");
}

// Add services to the container.
services.AddRazorPages();
services.AddServerSideBlazor();

services.AddSingleton<Demon>();
services.AddHostedService(provider => provider.GetService<Demon>());

services.AddSingleton<WeatherForecastService>();

services.AddControllersWithViews();
services.AddRazorPages();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();





// appsettings.json 파일을 읽어서 환경변수로 *먼저* 설정한다.
// windows service 에서는 batch file 로 구동하기 불편하고 까다롭기 때문에,
// Web 환경 초기화 되기 이전에 환경변수로 설정되어야 제대로 동작하므로,
// appsettings.json 파일을 *미리* 한번 더 읽어서 환경변수로 설정
static void PresetAppSettings(bool isWinService)
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? (isWinService ? "Production" : "Development");
    var config = new ConfigurationBuilder()
        .SetBasePath(isWinService ? AppContext.BaseDirectory : Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // 환경별 구성 파일 로드
        .Build()
        ;

    if (config["PRESET_ASPNETCORE_URLS"].NonNullAny())
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", config["PRESET_ASPNETCORE_URLS"]);
}

