using System.Net;
using System.Net.Sockets;

namespace Dual.Common.Core
{
    public static class NetEx
    {
        public static IPAddress GetLocalIpAddress()
        {
            // https://stackoverflow.com/questions/6803073/get-local-ip-address
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address;
        }

        // https://www.csharp-examples.net/local-ip/
        //bool b1 = IsLocalIpAddress("localhost");        // true (loopback name)
        //bool b2 = IsLocalIpAddress("127.0.0.1");        // true (loopback IP)
        //bool b3 = IsLocalIpAddress("MyNotebook");       // true (my computer name)
        //bool b4 = IsLocalIpAddress("192.168.0.2");      // true (my IP)
        //bool b5 = IsLocalIpAddress("NonExistingName");  // false (non existing computer name)
        //bool b6 = IsLocalIpAddress("99.0.0.1");         // false (non existing IP in my net)
        public static bool IsLocalIpAddress(string host)
        {
            try
            { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
