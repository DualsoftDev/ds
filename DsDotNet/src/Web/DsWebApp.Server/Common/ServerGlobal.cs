using Engine.Core;
using Engine.Info;
using Engine.Runtime;

using IO.Core;

using System.ComponentModel;

namespace DsWebApp.Server.Common
{
    public class ServerGlobal : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ServerSettings ServerSettings { get; set; }

        /// <summary>
        /// DsZipPath 에 따른 compile 된 Runtime model
        /// </summary>
        public RuntimeModel RuntimeModel { get; private set; }


        public DSCommonAppSettings DsCommonAppSettings { get; set; }
        internal DBLoggerORM.LogSet LogSet { get; set; }

        public ILog Logger { get; set; }

        static IoHub _ioHub;
        public ServerDirectAccess IoHubServer => _ioHub?.Server;

        public ServerGlobal(ServerSettings serverSettings, DSCommonAppSettings commonAppSettings, ILog logger)
        {
            ServerSettings = serverSettings;
            DsCommonAppSettings = commonAppSettings;
            Logger = logger;
            try
            {
                RuntimeModel = ReloadRuntimeModel();
                //if (serverSettings.AutoStartOnSystemPowerUp)
                //    Task.Factory.StartNew(() => RuntimeModel?.Cpu.Run());
                RuntimeModel?.Cpu.RunInBackground();
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to load runtime model: {serverSettings.RuntimeModelDsZipPath}\r\n{ex.Message}");
            }
        }

        public RuntimeModel ReloadRuntimeModel()
        {
            var dsZipPath = ServerSettings.RuntimeModelDsZipPath;
            try
            {
                Logger.Info($"Model change detected: {dsZipPath}");
                RuntimeModel?.Dispose();
                RuntimeModel = new RuntimeModel(dsZipPath);
                return RuntimeModel;
            }
            catch (Exception)
            {
                Logger.Error($"Failed to load model: {dsZipPath}");
                return null;
            }
        }


        public static void ReStartIoHub(string zmqSettingsJson)
        {
            _ioHub?.Dispose();
            _ioHub = new IoHub(zmqSettingsJson);
        }
    }
}
