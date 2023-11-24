using Blazored.LocalStorage;
using Blazored.SessionStorage;
using DsWebApp.Shared;
using Dual.Web.Blazor.Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

IServiceCollection services = builder.Services; 
services.AddDevExpressBlazor(options => {
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});
services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
services.AddScoped<DualWebBlazorJsInterop>();
services.AddScoped<CanvasJsInterop>();
services.AddScoped<FilesManager>();
services.AddScoped<ClientGlobal>();

services.AddBlazoredLocalStorage();
services.AddBlazoredSessionStorage();
services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
services.AddAuthorizationCore();

await builder.Build().RunAsync();