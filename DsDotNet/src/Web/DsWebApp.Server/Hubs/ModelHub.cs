namespace DsWebApp.Server.Hubs
{
    public class ModelHub : Hub
    {
        public static HashSet<string> ConnectedClients { get; } = new HashSet<string>();
        public ModelHub(IConfiguration configuration)
        {
            //GlobalCounter.TheGlobalCounter ??= configuration.GetSection("GlobalCounter").Get<GlobalCounter>();
        }

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
