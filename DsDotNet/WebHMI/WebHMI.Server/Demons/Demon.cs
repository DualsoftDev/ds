using Microsoft.AspNetCore.SignalR;
using WebHMI.Server.Hubs;

namespace WebHMI.Server.Demons;

public class Demon : BackgroundService
{
    readonly IHubContext<DsHub> _hubContextDS;

    public Demon(IHubContext<DsHub> hubContextDS)
    {
        _hubContextDS = hubContextDS;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Run(() => { });
        //
    }
}
