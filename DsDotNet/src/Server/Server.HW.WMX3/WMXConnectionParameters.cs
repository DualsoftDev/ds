using Server.HW.Common;
using System;

namespace Server.HW.WMX3;

public class WMXConnectionParameters : ConnectionParametersEtherCAT
{
    public WMXConnectionParameters(TimeSpan timeoutConnecting, TimeSpan timeoutScan)
    : base(timeoutConnecting, timeoutScan)
    {
    }
}
