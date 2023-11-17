using DsWebApp.Server.Controllers;
using DsWebApp.Shared;

using Microsoft.AspNetCore.SignalR;

namespace DsWebApp.Server.Hubs
{
    public class FieldIoHub(IConfiguration configuration) : Hub
    {
        public static HashSet<string> ConnectedClients = new HashSet<string>();

        public override Task OnConnectedAsync()
        {
            ConnectedClients.Add(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedClients.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
        public async Task IncrementCount(int increment)
        {
            //GlobalCounter.TheGlobalCounter.Count += increment;
            //await Clients.All.SendAsync(K.S2CNNIOChanged, GlobalCounter.TheGlobalCounter.Count);
        }


    }
}
