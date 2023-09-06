using Dual.Common.Core;
using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Net;
using System.Threading.Tasks;

namespace DSModeler
{
    public enum PaixHW
    {
        NMC2,
        NMF,
        WMX // WMX_ETHERCAT
    }

    public class PaixDriver
    {
        public string IP { get; set; }
        public ConnectionBase Conn { get; set; }
        private PaixHW _paixHW;
        public PaixDriver(PaixHW paixHW, string ip, int numIn, int numOut)
        {
            _paixHW = paixHW;
            IP = PaixHW.WMX == paixHW ? "127.0.0.1" : ip; //모벤시스 sw ethercat 로컬 PC만 작동하는 버전

            if (PaixHW.WMX == paixHW)
            {
                var connPara = new WMXConnectionParameters(Global.RunHWIP, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(50));
                Conn = new WMXConnection(connPara, numIn, numOut);
            }
            else if (PaixHW.NMC2 == paixHW) { /*...*/}
            else if (PaixHW.NMF == paixHW) { /*...*/}
        }

        public bool Open()
        {
            IPAddress.TryParse(IP, out IPAddress addr);
            if (addr == null) { MBox.Error($"{IP} ip 형식으로 올바르지 않습니다."); return false; }

            return Conn.Connect();
        }
        public bool Close()
        {
            return Conn.Disconnect();
        }

        public void Start()
        {
            if (!Conn.IsConnected)
                Open();

            if (!Conn.IsRunning)
            {
                Task.Run(async () =>
                {
                    await Conn.StartDataExchangeLoopAsync();
                });
            }
        }
        public void Stop()
        {
            if (Conn.IsRunning)
            {

                Conn.Tags.ForEach(tag => { tag.Value.WriteRequestValue = false; });
                Conn.StopDataExchangeLoop();
            }
        }
    }
}


