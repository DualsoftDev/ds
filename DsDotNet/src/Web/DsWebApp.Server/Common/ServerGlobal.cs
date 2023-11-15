using Engine.Core;
using Engine.Info;
using Engine.Runtime;

using System.ComponentModel;

namespace DsWebApp.Server.Common
{
    public class ServerGlobal : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ServerSettings ServerSettings { get; set; }

        public string DsZipPath { get; set; }
        /// <summary>
        /// DsZipPath 에 따른 compile 된 Runtime model
        /// </summary>
        public RuntimeModel RuntimeModel { get; private set; }

        public DSCommonAppSettings DsCommonAppSettings { get; set; }
        internal DBLoggerORM.LogSet LogSet { get; set; }

        public ILog Logger { get; set; }
    }
}
