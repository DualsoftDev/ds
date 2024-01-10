using DevExpress.XtraPrinting.Native.Lines;

using DsWebApp.Server.Hubs;

using Engine.Runtime;

using IO.Core;

using static Engine.Core.TagWebModule;
using static Engine.Core.InfoPackageModule;

using SK = DsWebApp.Shared.SK;
using static Engine.Core.CoreModule;
using Engine.Info;
using Engine.Core;
using static Engine.Core.Interface;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using System.Text.Json;
using static Engine.Info.DBLoggerORM;

namespace DsWebApp.Server.Demons;
public partial class Demon : BackgroundService
{
    ServerGlobal _serverGlobal;
    IHubContext<FieldIoHub> _hubContextFieldIo;
    IHubContext<InfoHub> _hubContextInfo;
    IHubContext<HmiTagHub> _hubContextHmiTag;
    ILog _logger => _serverGlobal.Logger;
    DsSystem _dsSystem => _serverGlobal.RuntimeModel?.System;

    public Demon(
        ServerGlobal serverGlobal
        , IHubContext<ModelHub> hubContextModel
        , IHubContext<FieldIoHub> hubContextFieldIo
        , IHubContext<InfoHub> hubContextInfo
        , IHubContext<HmiTagHub> hubContextHmiTag)
    {
        _serverGlobal = serverGlobal;
        _hubContextFieldIo = hubContextFieldIo;
        _hubContextHmiTag = hubContextHmiTag;
        _hubContextInfo = hubContextInfo;

        CompositeDisposable _modelSubscription = new();
        IDisposable innerSubscriptionFromWeb = null;
        IDisposable innerSubscriptionFromCpu = null;
        if (serverGlobal.RuntimeModel != null)
        {
            onRuntimeModelReady(serverGlobal.RuntimeModel);
        }
        serverGlobal.RuntimeModelChangedSubject.Subscribe(runtimeModel =>
        {
            onRuntimeModelReady(runtimeModel);

            hubContextModel.Clients.All.SendAsync(SK.S2CNModelChanged, serverGlobal.ServerSettings.RuntimeModelDsZipPath);
            hubContextModel.Clients.All.SendAsync(SK.S2CNCpuRunningStatusChanged, runtimeModel.Cpu.IsRunning);
        });

        void onRuntimeModelReady(RuntimeModel runtimeModel)
        {
            innerSubscriptionFromWeb?.Dispose();
            innerSubscriptionFromWeb = subscribeTagChangeWeb(runtimeModel);
            innerSubscriptionFromCpu?.Dispose();
            innerSubscriptionFromCpu = subscribeTagChangeCpu(runtimeModel);


            DsSystem[] systems = [ runtimeModel.System ];
            ModelCompileInfo mci = new(runtimeModel.SourceDsZipPath, runtimeModel.SourceDsZipPath);

            _modelSubscription.Clear();
            var querySet = new QuerySet(DateTime.Now.AddDays(-1), null) { CommonAppSettings = _serverGlobal.DsCommonAppSettings };
            var logSetW = DBLogger.InitializeLogReaderWriterOnDemandAsync(querySet, _serverGlobal.DsCommonAppSettings, systems, mci).Result;
            _modelSubscription.Add(logSetW);
            IDisposable subscription =
                CpusEvent.ValueSubject
                    .Subscribe(tpl =>
                    {
                        var (sys, storage, val) = (tpl.Item1, tpl.Item2, tpl.Item3);
                        var ti = storage.GetTagInfo();
                        if (ti != null && ti.Value.IsNeedSaveDBLog())
                            DBLogger.EnqueLogForInsert(new DsLogModule.DsLog(DateTime.Now, storage));
                    });
            _modelSubscription.Add(subscription);


        }

        IDisposable subscribeTagChangeWeb(RuntimeModel runtimeModel)
        {
            return runtimeModel.Cpu.TagWebChangedFromWebSubject.Subscribe(tagWeb =>
            {
                _logger.Debug($"Server: Notifying TagWeb({tagWeb.Name}) change from Web to all clients");
                hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
            });
        }
        IDisposable subscribeTagChangeCpu(RuntimeModel runtimeModel)
        {
            return runtimeModel.Cpu.TagWebChangedFromCpuSubject.Subscribe(tagWeb =>
            {
                _logger.Debug($"Server: Notifying TagWeb({tagWeb.Name}) change from CPU to all clients");
                hubContextHmiTag.Clients.All.SendAsync(SK.S2CNTagWebChanged, tagWeb);
            });
        }
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

                        if (n % 3 == 0)
                            Task.Run(async () =>
                            {
                                if (_dsSystem != null && InfoHub.ConnectedClients.Any())
                                {
                                    Console.WriteLine($"HmiTagHub has {InfoHub.ConnectedClients.Count} connected clients.");
                                    InfoSystem infoSystem = InfoPackageModuleExt.GetInfo(_dsSystem);
                                    // System.Text.Json.JsonSerializer.Serialize 는 동작 안함.
                                    var newtonJson = Newtonsoft.Json.JsonConvert.SerializeObject(infoSystem);
                                    await _hubContextInfo.Clients.All.SendAsync(SK.S2CNInfoChanged, newtonJson);
                                }
                                else
                                    _logger.Debug("No InfoHub clients connected");
                            }).FireAndForget();

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
