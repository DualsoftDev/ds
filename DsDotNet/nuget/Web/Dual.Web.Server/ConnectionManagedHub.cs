using Microsoft.AspNetCore.SignalR;

namespace Dual.Web.Server;

using ClientId = string;
using HubName = string;

/// <summary>
/// Server side 에서 Hub 에 연결된 client 관리 기능이 추가된 Hub
/// </summary>
public abstract class ConnectionManagedHub(HubName hubName) : Hub
{
    static Dictionary<HubName, HashSet<ClientId>> _connectedClientMap = new();
    public static HashSet<ClientId> GetClients(string hubName)
    {
        if (_connectedClientMap.TryGetValue(hubName, out var clients))
            return clients;
        return new();
    }

    public override Task OnConnectedAsync()
    {
        var useId = Context.UserIdentifier;
        var userName = Context.User.Identity.Name;
        Console.WriteLine($"Hub client connected on {hubName} {Context.UserIdentifier}, {Context.User.Identity.Name}");
        HashSet<ClientId> clients;
        if (! _connectedClientMap.TryGetValue(hubName, out clients))
            _connectedClientMap[hubName] = clients = new();

        clients.Add(Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        Console.WriteLine($"Hub client disconnected on {hubName}");

        if (_connectedClientMap.TryGetValue(hubName, out var clients))
            clients.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
