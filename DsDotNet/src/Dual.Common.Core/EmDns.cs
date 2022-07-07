using System.Net;
using System.Net.Sockets;

namespace Lpb.Common
{
    public static class EmDns
    {
        public static IPAddress GetLocalIpAddress()
        {
            // https://stackoverflow.com/questions/6803073/get-local-ip-address
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address;
        }
    }
}
