using Microsoft.Extensions.Hosting.WindowsServices;

using Dual.Web.Blazor.ServerSide;
using DsWebApp.Server.Demons;
using Dual.Common.Core.FS;      // for F# common logger setting
using Engine.Core;
using Engine.Info;
using Dual.Web.Server.Auth;
using Microsoft.Data.Sqlite;
using Dual.Web.Blazor.ClientSide;
using Microsoft.AspNetCore.StaticFiles;
using static Engine.Info.DBLoggerORM;

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
Dual.Common.Core.Log4NetLogger.Logger = logger;
logger.Debug($"==================== DEBUG");
// Add services to the container.
services.AddSignalR();
conf.AddEnvironmentVariables();

var env = conf["ASPNETCORE_ENVIRONMENT"];
logger.Info($"ASPNETCORE_ENVIRONMENT = {env}");

var urls = conf["ASPNETCORE_URLS"];
logger.Info($"ASPNETCORE_URLS = {urls}");



if (!isWinService)
{
    builder.Host.UseContentRoot(Directory.GetCurrentDirectory());
    Debug.WriteLine($"Current = {Directory.GetCurrentDirectory()}");
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

var commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));
try { commonAppSettings.LoggerDBSettings.FillModelId(); }
catch (Exception ex) { logger.Error($"Failed to initialize LoggerDB setting: {ex}"); }

var serverSettings =
    conf.GetSection("ServerSettings").Get<ServerSettings>()
        .Tee(ss =>
        {
            try { ss.Initialize(commonAppSettings); }
            catch (Exception ex) { logger.Error($"Failed to initialize LoggerDB setting: {ex}"); }
        })
        ;
var serverGlobals = new ServerGlobal(serverSettings, commonAppSettings, logger);
services.AddSingleton(serverGlobals);
services.AddDsAuth(serverGlobals, conf);

await services.AddUnsafeServicesAsync(serverGlobals, logger);


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
// Remove This
// app.UseStaticFiles();

// Add This
var provider = new FileExtensionContentTypeProvider();
provider.Mappings.Remove(".data");
provider.Mappings[".data"] = "application/octet-stream";
provider.Mappings.Remove(".wasm");
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings.Remove(".symbols.json");
provider.Mappings[".symbols.json"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });
app.UseStaticFiles();
//--------------

app.UseRouting();

app.UseCors(_corsPolicyName);

app.UseAuthentication();
app.UseAuthorization();


app.MapRazorPages();
app.MapControllers();

app.MapHub<FieldIoHub>(FieldIoHub.HubPath);
app.MapHub<InfoHub>(InfoHub.HubPath);
app.MapHub<ModelHub>(ModelHub.HubPath);
app.MapHub<HmiTagHub>(HmiTagHub.HubPath)
    .RequireCors(_corsPolicyName);
app.MapHub<DbHub>(DbHub.HubPath);

app.MapFallbackToFile("index.html");
app.UseWebSockets(); // WebSocket 활성화



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
    public static async Task<IServiceCollection> AddUnsafeServicesAsync(this IServiceCollection services, ServerGlobal serverGlobal, ILog logger)
    {
        if (serverGlobal.DsCommonAppSettings.LoggerDBSettings.ModelId >= 0)
            await DBLogger.InitializeLogDbOnDemandAsync(serverGlobal.DsCommonAppSettings, cleanExistingDb:false);
            //var connectionString = commonAppSettings.LoggerDBSettings.ConnectionString;
            //var dsFileJson = DBLogger.GetDsFilePath(connectionString);

        ServerGlobal.ReStartIoHub(Path.Combine(AppContext.BaseDirectory, "zmqsettings.json"));
        return services;
    }

    class User : UserAuthInfo
    {
        public string Roles { get; set; }
    }
    public static IServiceCollection AddDsAuth(this IServiceCollection services, ServerGlobal serverGlobal, ConfigurationManager conf)
    {
        Func<string, UserAccount> userInfoExtractor = (string userName) =>
        {
            using var conn = serverGlobal.CreateDbConnection();
            var user = conn.QueryFirstOrDefault<User>("SELECT * FROM [user] WHERE [username] = @UserName;", new { UserName = userName });
            if (user is null)
                return null;
            var roles = user.IsAdmin ? "Administrator" : "User";
            if (user.Roles.NonNullAny())
                roles += "," + user.Roles;
            var userAccount = new UserAccount() { UserName = userName, Roles = roles };
            if (user.Password is null)
                return userAccount;
            userAccount.Password = Dual.Common.Utils.Crypto.Decrypt(user.Password, K.CryptKey);
            return userAccount;
        };

        UserAccountService svc = new(userInfoExtractor);
        svc.JwtTokenValidityMinutes = serverGlobal.ServerSettings.JwtTokenValidityMinutes;
        services.AddAuth();
        services.AddSingleton((IUserAccountService)svc);
        return services;
    }
}
