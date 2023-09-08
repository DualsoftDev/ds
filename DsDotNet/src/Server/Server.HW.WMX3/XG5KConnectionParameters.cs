using Server.HW.Common;
using System;

namespace Server.HW.XG5K;

public class XG5KConnectionParameters : ConnectionParametersEtherCAT
{
    public XG5KConnectionParameters(string ip, TimeSpan timeoutConnecting, TimeSpan timeoutScan)
    : base(ip, timeoutConnecting, timeoutScan)
    {
    }
}
