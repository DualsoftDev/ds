using System;

namespace Server.HW.Common
{
    public abstract class ConnectionParametersEtherCAT : IConnectionParametersEtherCAT
    {
        public TimeSpan TimeoutConnecting { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan TimeoutScan { get; set; } = TimeSpan.FromMilliseconds(50);
        public string IP { get; set; } = "";


        public ConnectionParametersEtherCAT(string ip, TimeSpan timeoutConnecting, TimeSpan timeoutScan)
        {
            TimeoutConnecting = timeoutConnecting;
            TimeoutScan = timeoutScan;
            IP = ip;    
        }
    }
}
