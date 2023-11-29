using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Blazored.Toast;

using DsWebApp.Shared;
using Dual.Web.Blazor.Client.Auth;
using Dual.Web.Blazor.Shared;

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

var clinetGlobal = new ClientGlobal();
services.AddSingleton<ClientGlobal>(clinetGlobal);
services.AddSingleton<ClientGlobalBase>(clinetGlobal);

services.AddBlazoredLocalStorage();
services.AddBlazoredSessionStorage();
services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
services.AddAuthorizationCore();

services.AddBlazoredToast();


await builder.Build().RunAsync();