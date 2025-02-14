using System;

namespace Dual.PLC.Common
{
    /// <summary>
    /// PLC connection base interface
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
    /// S/W PLC connection interface
    /// </summary>
    public interface IConnectionParametersSimulator : IConnectionParameters
    {        
    }

    /// <summary>
    /// Ethernet PLC connection interface
    /// </summary>
    public interface IConnectionParametersEthernet : IConnectionParameters
    {
        string Ip { get; }
        ushort Port { get;}
        TimeSpan Timeout { get; set; }
        TransportProtocol TransportProtocol { get; }
    }

    /// <summary>
    /// USB PLC connection interface
    /// </summary>
    public interface IConnectionParametersUSB : IConnectionParameters
    {
        
    }
}
