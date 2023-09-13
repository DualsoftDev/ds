using Server.HW.Common;
using System;

namespace Server.HW.XG5K;

public class XG5KConnectionParameters : ConnectionParametersEthernet
{
    public XG5KConnectionParameters(TimeSpan timeoutConnecting, string ip, ushort port = 2004)
    : base(ip, port, TransportProtocol.Tcp)
    {
        this.Timeout = timeoutConnecting;
    }
}
