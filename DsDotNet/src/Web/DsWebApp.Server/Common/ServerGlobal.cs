using Engine.Core;
using Engine.Info;
using Engine.Runtime;

namespace DsWebApp.Server.Common
{
    public class ServerGlobal
    {
        public ServerSettings ServerSettings { get; set; }
        public RuntimeModel RuntimeModel { get; set; }

        public DSCommonAppSettings DsCommonAppSettings { get; set; }
        internal DBLoggerORM.LogSet LogSet { get; set; }
    }
}
