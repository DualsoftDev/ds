using DsWebApp.Server.Controllers;
using DsWebApp.Shared;

using Microsoft.AspNetCore.SignalR;

namespace DsWebApp.Server.Hubs
{
    public class FieldIoHub : Hub
    {
        public FieldIoHub(IConfiguration configuration)
        {
            //GlobalCounter.TheGlobalCounter ??= configuration.GetSection("GlobalCounter").Get<GlobalCounter>();
        }
        public async Task IncrementCount(int increment)
        {
            //GlobalCounter.TheGlobalCounter.Count += increment;
            //await Clients.All.SendAsync("IoMemoryChanged", GlobalCounter.TheGlobalCounter.Count);
        }


    }
}
