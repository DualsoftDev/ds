using DsWebApp.Server.Hubs;

using IO.Core;

using static Engine.Core.TagWebModule;

using SK = DsWebApp.Shared.SK;

namespace DsWebApp.Server.Demons;

public partial class Demon : BackgroundService
{
    ServerGlobal _serverGlobal;
    IHubContext<FieldIoHub> _hubContextFieldIo;
    IHubContext<HmiTagHub> _hubContextHmiTag;
    ILog _logger => _serverGlobal.Logger;

    public Demon(ServerGlobal serverGlobal, IHubContext<FieldIoHub> hubContextFieldIo, IHubContext<HmiTagHub> hubContextHmiTag)
    {
        _serverGlobal = serverGlobal;
        _hubContextFieldIo = hubContextFieldIo;
        _hubContextHmiTag = hubContextHmiTag;

        serverGlobal.RuntimeModel?.Cpu.TagWebChangedSubject.Subscribe(tagWeb =>
        {
            _logger.Debug("Server: Notifying TagWeb change to all clients");

            // "hub/hmitag"
            hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
        });
    }
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
                        //    Task.Run(async () =>
                        //    {
                        //        if (HmiTagHub.ConnectedClients.TryGetValue("HmiTagHub", out var clients) && clients.Any())
                        //        {
                        //            Console.WriteLine($"HmiTagHub has {clients.Count} connected clients.");
                        //            await _hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, new TagWeb("Test", true, 999));
                        //        }
                        //        else
                        //            _logger.Debug("No HmiTagHub clients connected");
                        //    }).FireAndForget();

                        //if (n % 10 == 0)
                        //    Task.Run(async () => { await checkAssetHealthAsync(n); }).FireAndForget();

                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error on Background Service:\r\n{ex}");
                    }
                });
        compositeDisposable.Add(subscription);
        var xx = _serverGlobal.IoHubServer.MemoryChangedObservable;
        subscription = _serverGlobal.IoHubServer.MemoryChangedObservable.Subscribe(change =>
        {
            try
            {
                if (FieldIoHub.ConnectedClients.Any())
                {
                    var simple = change.ToSimple();
                    switch (simple)
                    {
                        case SimpleNumericIOChangeInfo c:
                            _hubContextFieldIo.Clients.All.SendAsync(K.S2CNNIOChanged, c);
                            break;
                        case SimpleSingleStringChangeInfo c:
                            _hubContextFieldIo.Clients.All.SendAsync(K.S2CNSIOChanged, c);
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
