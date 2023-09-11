using WebHMI.Server.Demons;
using WebHMI.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Add services to the container.
services.AddSignalR();
builder.Configuration.AddEnvironmentVariables();
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());
services.AddSingleton<Demon>();
//services.AddHostedService(provider => provider.GetService<Demon>());
services.AddHostedService(provider => provider.GetRequiredService<Demon>()); // exception log 필요

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

        policy.WithOrigins("http://localhost:*")
                .AllowAnyMethod()
                .AllowAnyHeader()
                ;
    });
});

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDevExpressBlazor(options => {
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});
builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseWebAssemblyDebugging();
} else {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCors(_corsPolicyName);

app.MapRazorPages();
app.MapControllers();
app.MapHub<DsHub>("/hub/ds")
    .RequireCors(_corsPolicyName);
app.MapFallbackToFile("index.html");

app.Run();