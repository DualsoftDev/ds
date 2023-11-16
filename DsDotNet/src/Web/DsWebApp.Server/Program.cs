using Microsoft.Extensions.Hosting.WindowsServices;

using Dual.Web.Blazor.ServerSide;
using DsWebApp.Server.Demons;
using DsWebApp.Server.Hubs;
using Dual.Common.Core.FS;      // for F# common logger setting
using Engine.Core;
using Engine.Info;

//using DsWebApp.Server.Authentication;

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
ILog logger = services.AddLog4net("DsWebAppServerLogger");
services.AddTraceLogAppender("DsWebAppServerLogger");
logger.Info($"======================= DsWebApp started.");
logger.Info($"Debugger.IsAttached = {Debugger.IsAttached}");
Log4NetWrapper.SetLogger(logger);

// Add services to the container.
services.AddSignalR();
conf.AddEnvironmentVariables();

var urls = conf["ASPNETCORE_URLS"];
logger.Info($"ASPNETCORE_URLS = {urls}");



if (!isWinService)
{
    builder.Host.UseContentRoot(Directory.GetCurrentDirectory());
    Trace.WriteLine($"Current = {Directory.GetCurrentDirectory()}");
}
services.AddSingleton<Demon>();
services.AddHostedService(provider => provider.GetService<Demon>());


services.AddHttpContextAccessor();

// Add services required for using options
services.AddOptions();

const string _corsPolicyName = "CorsPolicy";

// https://www.syncfusion.com/faq/blazor/general/how-do-you-enable-cors-in-a-blazor-server-application
services.AddCors(options =>
{
    options.AddPolicy(_corsPolicyName, policy =>
    {
        policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                ;

        var urls = new[] {  "http://localhost:*", "http://192.168.9.118:*",
                            "https://localhost:*", "https://192.168.9.118:*",
                            "tcp://localhost:*", "tcp://"
        };
        foreach (var url in urls)
        {
            policy.WithOrigins(url)
                .AllowAnyMethod()
                .AllowAnyHeader()
                ;
        }
    });
});



services.AddControllersWithViews()
    //.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonIntToStringConverter()))
    ;
//services.AddMvc().AddApplicationPart(Assembly.Load(new AssemblyName("Dual.Web.Blazor.Server.Auth")));
services.AddRazorPages();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddDevExpressBlazor(options =>
{
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});

var serverSettings =
    conf.GetSection("ServerSettings").Get<ServerSettings>()
        .Tee(ss => ss.Initialize());
var serverGlobals = new ServerGlobal() { ServerSettings = serverSettings, Logger = logger };

services.AddSingleton(serverGlobals);


await services.InitializeUnsafeServicesAsync(serverGlobals, logger);


builder.WebHost.UseStaticWebAssets();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();


app.UseErrorHandlingMiddleware();   // ErrorHandlingMiddleware.cs

if (serverSettings.UseHttpsRedirection)
    app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCors(_corsPolicyName);


app.MapRazorPages();
app.MapControllers();
app.MapHub<VanillaHub>("/hub/vanilla")
    .RequireCors(_corsPolicyName);
app.MapHub<FieldIoHub>("/hub/io");

app.MapFallbackToFile("index.html");

logger.Info($"--- DsWebApp setup finished.  now running...");

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

public static class CustomServerExtension
{
    public static async Task<IServiceCollection> InitializeUnsafeServicesAsync(this IServiceCollection services, ServerGlobal serverGlobal, ILog logger)
    {
        var commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));
        await DBLogger.InitializeLogDbOnDemandAsync(commonAppSettings);
        //var connectionString = commonAppSettings.LoggerDBSettings.ConnectionString;
        //var dsFileJson = DBLogger.GetDsFilePath(connectionString);

        ServerGlobal.ReStartIoHub(Path.Combine(AppContext.BaseDirectory, "zmqsettings.json"));

        serverGlobal.DsCommonAppSettings = commonAppSettings;

        CompositeDisposable modelChangeDisposables = new();
        serverGlobal.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(serverGlobal.DsZipPath))
            {
                modelChangeDisposables.Dispose();
                K.Noop();

                ///serverGlobal.RuntimeModel = ....;
                logger.Info($"Model change detected: {serverGlobal.DsZipPath}");
                // todo : 모델 변경에 따른 작업 수행
                // 1. DBLogger storage table 변경
                //

                modelChangeDisposables = new();
            }
        };


        return services;
    }
}
