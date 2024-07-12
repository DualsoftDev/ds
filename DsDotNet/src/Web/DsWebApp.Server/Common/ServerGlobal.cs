using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using IO.Core;

using Microsoft.Data.Sqlite;

using System.Data;
using System.Reactive.Subjects;
using static Engine.Core.RuntimeGeneratorModule;

namespace DsWebApp.Server.Common;

public class ServerGlobal
{
    public ServerSettings ServerSettings { get; set; }
    public bool ServerReady { get; set; }
    public DSCommonAppSettings DsCommonAppSettings { get; set; }

    /// <summary>
    /// DsZipPath 에 따른 compile 된 Runtime model
    /// </summary>
    public RuntimeModel RuntimeModel { get; private set; }
    public Subject<RuntimeModel> RuntimeModelChangedSubject { get; } = new();


    internal DBLoggerORM.LogSet LogSet { get; set; }

    public ILog Logger { get; set; }

    static IoHub _ioHub;
    public ServerDirectAccess IoHubServer => _ioHub?.Server;

    public ServerGlobal(ServerSettings serverSettings, DSCommonAppSettings commonAppSettings, ILog logger)
    {
        ServerSettings = serverSettings;
        DsCommonAppSettings = commonAppSettings;
        Logger = logger;
        Task.Run(() =>  // 최초 browser open 을 빨리 수행할 수 있도록 background 에서 실행
        {
            try
            {
                RuntimeModel = ReloadRuntimeModel(serverSettings);
                if (serverSettings.AutoStartOnSystemPowerUp)
                {
                    // Task.Factory.StartNew(() => RuntimeModel?.Cpu.Run());
                    RuntimeModel?.Cpu.RunInBackground();
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to load runtime model: {serverSettings.RuntimeModelDsZipPath}\r\n{ex.Message}");
            }
        }
        );
    }

    public RuntimeModel ReloadRuntimeModel(ServerSettings serverSettings)
    {
        var dsZipPath = ServerSettings.RuntimeModelDsZipPath;
        try
        {
            Logger.Info($"Model change detected: {dsZipPath}");
            RuntimeModel?.Dispose();
            RuntimeModel = null;

            if (!File.Exists(dsZipPath))
            {
                Logger.Warn($"Model file not found: {dsZipPath}");
                return null;
            }

            RuntimeDS.Package = serverSettings.GetRuntimePackage();

            RuntimeModel = new RuntimeModel(dsZipPath, PlatformTarget.WINDOWS);

            RuntimeModelChangedSubject.OnNext(RuntimeModel);
            return RuntimeModel;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load model: {dsZipPath}\r\n{ex.Message}");
            return null;
        }
    }


    public static void ReStartIoHub(string zmqSettingsJson)
    {
        _ioHub?.Dispose();
        _ioHub = new IoHub(zmqSettingsJson);
    }

    public IDbConnection CreateDbConnection() =>
        new SqliteConnection($"Data Source={DsCommonAppSettings.LoggerDBSettings.ConnectionPath}")
        .Tee(c => c.Open());

    /// <summary>
    /// wait for the cache to be initialized 
    /// </summary>
    public async Task<bool> StandbyUntilServerReadyAsync(int seconds = 10)
    {
        for (int i = 0; i < seconds && !ServerReady; i++)
        {
            await Console.Out.WriteLineAsync($"Waiting server ready: {i}...");
            await Task.Delay(1000);
        }

        if (!ServerReady)
            Logger.Error("Server not ready!");

        return ServerReady;
    }
}
