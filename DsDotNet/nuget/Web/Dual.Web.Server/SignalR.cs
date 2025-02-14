// https://www.youtube.com/watch?v=pNfSOBzHd8Y

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.SignalR;

namespace Dual.Web.Server;

public static class SignalR<T> where T: Hub
{
    public static string HubUrl { get; set; }
    public static void Configure(string url)
    {
        HubUrl = url;
        var builder = WebHost.CreateDefaultBuilder();
        builder.UseStartup<StartUp<T>>();

        builder.Build().Run();
    }
}

public class StartUp<T> where T : Hub
{
    public StartUp(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; set; }
    public void ConfigureServices(IServiceCollection services) => services.AddSignalR();
    public void Configure(IApplicationBuilder app)
    {
        //app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapHub<T>(SignalR<T>.HubUrl ! ));
    }
}

public class SampleHub : Hub
{
    public async Task SendMessage(string userName, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", userName, message);
    }

}

/*
public class ClientSample
{
    // Microsoft.AspNetCore.SignalR.Client 참조 추가
    public async Task DoAsync()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5000/samplehub")
            .Build();
        connection.On("ReceiveMessage", (string userName, string message) =>
        {
            Console.WriteLine($"{userName}: {message}");
        })
        await connection.StartAsync();
        await connection.InvokeCoreAsync("SendMessage", args: new[] { "Scribo", "Hello" });

    }
}
*/
