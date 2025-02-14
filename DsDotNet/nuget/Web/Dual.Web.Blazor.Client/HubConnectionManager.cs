using Microsoft.AspNetCore.SignalR.Client;

namespace Dual.Web.Blazor.Client;

public class HubConnectionManager : IAsyncDisposable
{
    HubConnection _hubConnection;
    IDisposable _subscription;

    public HubConnectionManager(HubConnection hubConnection, Func<HubConnection, IDisposable> hubSubscriber)
    {
        _hubConnection = hubConnection;
        _subscription = hubSubscriber.Invoke(hubConnection);
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.StopAsync();
        await _hubConnection.DisposeAsync();
        _subscription.Dispose();

        _hubConnection = null;
        _subscription = null;
    }
}


/// <summary>
/// Mutiple HubConnectionManager.  동일 page 내에서 다중 hub 접속이 필요한 경우 사용.
/// </summary>
public class HubConnectionsManager : IAsyncDisposable
{
    HubConnectionManager[] _hubConnectionManagers;

    public HubConnectionsManager( IEnumerable<(HubConnection, Func<HubConnection, IDisposable>)> hubConnectionsAndSubscribers)
    {
        _hubConnectionManagers =
            hubConnectionsAndSubscribers.Select(tpl =>
            {
                HubConnection hubConnection = tpl.Item1;
                Func<HubConnection, IDisposable> hubSubscriber = tpl.Item2;
                return new HubConnectionManager(hubConnection, hubSubscriber);
            }).ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var hubConnectionManager in _hubConnectionManagers)
            await hubConnectionManager.DisposeAsync();
        _hubConnectionManagers = null;
    }
}
