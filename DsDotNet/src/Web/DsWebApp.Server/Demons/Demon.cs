using Dual.Common.Core;
using Dual.Common.Db;

//using DsWebApp.Server.Common;
using DsWebApp.Server.Controllers;

namespace DsWebApp.Server.Demons;

public partial class Demon : BackgroundService
{
    readonly ILog _logger;
    //DbRepository _repository;
    //readonly IHubContext<VanillaHub> _hubContext;
    //UnsafeServices _unsafeServices;
    public Demon(
        ILog logger
        //, DbRepository repository
        //, IHubContext<VanillaHub> hubContext
        //, UnsafeServices unsafeServices
        )
    {
        _logger = logger;
        //_repository = repository;
        //_hubContext = hubContext;
        //_unsafeServices = unsafeServices;
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
        //await DbCacheController.InitializeAsync(_logger, _repository);

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

        ct.Register(() => subscription.Dispose());
    }


}
