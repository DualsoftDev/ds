
namespace DSModeler.HW
{
   

    public class DsDriver
    {
        public string IP { get; set; }
        public ConnectionBase Conn { get; set; }
        private readonly HwModel _modelHW;
        public DsDriver(HwModel modelHW, string ip, int numIn, int numOut)
        {
            _modelHW = modelHW;
            IP = ip;

            if (modelHW.Company == Company.LSE)
            {
                XG5KConnectionParameters connPara = new(TimeSpan.FromMilliseconds(50), Global.RunHWIP);
                Conn = new XG5KConnection(connPara, 200,  numIn, numOut);
            }
            else if (modelHW.Company == Company.PAIX) { /*...*/}
        }

        public bool Open()
        {
            _ = IPAddress.TryParse(IP, out IPAddress addr);
            if (addr == null && OperatingSystem.IsWindows()) { _ = MBox.Error($"{IP} ip 형식으로 올바르지 않습니다."); return false; }

            return Conn.Connect();
        }
        public bool Close()
        {
            return Conn.Disconnect();
        }

        public void Start()
        {
            if (!Conn.IsConnected)
            {
                _ = Open();
            }

            if (!Conn.IsRunning)
            {
                if (_modelHW.Company == Company.LSE)
                    ((XG5KConnection)Conn).Start();
                else
                    _ = Task.Run(Conn.StartDataExchangeLoopAsync);
            }
        }

      
        public void Stop()
        {
            if (Conn.IsRunning)
            {
                Conn.Tags.Iter(tag => { tag.Value.WriteRequestValue = false; });
                Conn.StopDataExchangeLoop();
            }
        }
    }
}


