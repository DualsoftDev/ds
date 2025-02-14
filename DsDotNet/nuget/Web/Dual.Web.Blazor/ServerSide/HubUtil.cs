using Microsoft.AspNetCore.SignalR.Client;

namespace Dual.Web.Blazor.ServerSide;

public static class HubExtension
{
    public static async Task<HubConnection> StartHubAsync(this Uri uri)
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(uri)
            .Build();

        await hubConnection.StartAsync();
        return hubConnection;
    }

    /// <summary>
    /// Hub 의 주어진 groupName group 에 가입
    /// Hub 구현에 "JOIN" 메소드가 존재해야 한다.
    /// </summary>
    public static async Task Join(this HubConnection hubConnection, string groupName) =>
        await hubConnection.InvokeAsync("JOIN", groupName);

    /// <summary>
    /// Hub 구현에 "LEAVE" 메소드가 존재해야 한다.
    /// </summary>
    public static async Task Leave(this HubConnection hubConnection, string groupName) =>
        await hubConnection.InvokeAsync("LEAVE", groupName);
}



