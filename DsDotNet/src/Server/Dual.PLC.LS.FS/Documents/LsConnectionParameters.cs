using Dsu.PLC.Common;

namespace Dsu.PLC.LS
{
    public class LsConnectionParameters : ConnectionParametersEthernet
    {
        public LsConnectionParameters(string ip, ushort port = 2004)
            : base(ip, port) { }
    }
}
