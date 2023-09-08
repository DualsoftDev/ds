using Server.HW.Common;
using System;

namespace Server.HW.WMX3;

public class WMXConnectionParameters : ConnectionParametersEtherCAT
{
    public WMXConnectionParameters(string ip, TimeSpan timeoutConnecting, TimeSpan timeoutScan)
    : base(ip, timeoutConnecting, timeoutScan)
    {
    }
}
