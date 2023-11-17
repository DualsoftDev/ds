using DsWebApp.Server.Hubs;

using IO.Core;

using K = DsWebApp.Shared.K;

namespace DsWebApp.Server.Demons;

public partial class Demon(ServerGlobal serverGlobal, IHubContext<FieldIoHub> hubContext) : BackgroundService
{
    ILog _logger => serverGlobal.Logger;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await executeAsyncHelper(ct);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error on Demon background service:\r\n{ex}");
        }
    }

    async Task executeAsyncHelper(CancellationToken ct)
    {
        await Task.Yield();
        //await DbCacheController.InitializeAsync(_logger, _repository);

        CompositeDisposable compositeDisposable = new();
        ct.Register(() => compositeDisposable.Dispose());

        IDisposable subscription =
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(n =>
                {
                    try
                    {
                        if (n % 60 == 0)
                            _logger.Debug($"{n / 60}: Background Service is working..");

                        //if (n % 2 == 0)
                        //    Task.Run(async () => { await monitorDatabaseAsync(n); }).FireAndForget();

                        //if (n % 10 == 0)
                        //    Task.Run(async () => { await checkAssetHealthAsync(n); }).FireAndForget();

                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error on Background Service:\r\n{ex}");
                    }
                });
        compositeDisposable.Add(subscription);
        var xx = serverGlobal.IoHubServer.MemoryChangedObservable;
        subscription = serverGlobal.IoHubServer.MemoryChangedObservable.Subscribe(change =>
        {
            try
            {
                if (FieldIoHub.ConnectedClients.Any())
                {
                    var simple = change.ToSimple();
                    switch (simple)
                    {
                        case SimpleNumericIOChangeInfo c:
                            hubContext.Clients.All.SendAsync(K.S2CNNIOChanged, c);
                            break;
                        case SimpleSingleStringChangeInfo c:
                            hubContext.Clients.All.SendAsync(K.S2CNSIOChanged, c);
                            break;
                        default:
                            throw new Exception($"Unknown IoMemoryChanged type: {change.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error on MemoryChanged:\r\n{ex}");
            }
        });
        compositeDisposable.Add(subscription);
    }


}
