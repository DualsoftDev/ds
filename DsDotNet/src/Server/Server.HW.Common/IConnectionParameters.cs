using System;

namespace Server.HW.Common
{
    /// <summary>
    /// HW connection base interface
    /// </summary>
    public interface IConnectionParameters
    {
        
    }

    /// <summary>
    /// Ethernet transport protocol enumerations
    /// </summary>
    public enum TransportProtocol
    {
        Tcp, Udp
    }

    /// <summary>
    /// S/W HW connection interface
    /// </summary>
    public interface IConnectionParametersSimulator : IConnectionParameters
    {        
    }

    /// <summary>
    /// Ethernet HW connection interface
    /// </summary>
    public interface IConnectionParametersEthernet : IConnectionParameters
    {
        string Ip { get; }
        ushort Port { get;}
        TimeSpan Timeout { get; set; }
        TransportProtocol TransportProtocol { get; }
    }

    /// <summary>
    /// EtherCAT HW connection interface
    /// </summary>
    public interface IConnectionParametersEtherCAT : IConnectionParameters
    {
        TimeSpan TimeoutConnecting { get; set; } 
        TimeSpan TimeoutScan { get; set; }

    }

    /// <summary>
    /// USB HW connection interface
    /// </summary>
    public interface IConnectionParametersUSB : IConnectionParameters
    {
        int VendorID { get; }
        int ProductID { get; }

    }
}
