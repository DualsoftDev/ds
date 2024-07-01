using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using RedisPLC;
using StackExchange.Redis;
using XGTComm;

namespace RedisProducer
{
    class Program
    {
        //PlC 정보
        public const int MAX_RANDOM_WRITE_POINTS = 64;
        public const int MAX_RANDOM_READ_POINTS = 64;
        public const int MAX_ARRAY_BYTE_SIZE = 512; // 64 * 8
        public const string _XGK_IP = "127.0.0.1:2004";

        //Json 정보
        //string jsonString = JsonSerializer.Serialize(weatherForecast);
        //DsData helloDSData = JsonConvert.DeserializeObject<DsData>(jsonString);
        

        // 구독할 채널 및 발행할 채널 설정
        static string subscribeChannel = "g2d";
        static string publishChannel = "d2g";

        static async Task Main(string[] args)
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var db = redis.GetDatabase();
            var sub = redis.GetSubscriber();

            //var groupedDataArray = GroupDsInterfacesByDevice(helloDSData);

            XGTConnection conn = new XGTConnection(_XGK_IP, true);



            // 메시지 수신 이벤트 핸들러 설정
            sub.Subscribe(subscribeChannel, (channel, message) =>
            {
                Console.WriteLine($" [x] Received from Graphic: {message}");
                int pongAddr = MessageToBitOffset(message);
                XGIDeviceWriteTest(conn, pongAddr);
            });

#pragma warning disable 4014
            Task.Run(async () => 
            {
                while(true)
                {
                    XGKDeviceReadTest(conn);
                    await Task.Delay(100);
                }
            });
#pragma warning restore 4014

            Console.WriteLine("DS started. Type messages to send to Graphic. Type 'exit' to quit.");
        }

        static int MessageToBitOffset(string message)
        {
            int addr = 0;


            return addr;
        }

        static public void XGKDeviceReadTest(XGTConnection conn)
        {
            var lst = new List<XGTDevice>();
            for (int i = 16; i < 20; i++) lst.Add(new XGTDeviceByte('M', i * 8));
            _ = conn.ReadRandomDevice(lst.ToArray());

            for (int i = 0; i < lst.Count; i += 10)
            {
                Debug.WriteLine(string.Join(", ", lst.Skip(i).Take(10).Select(f => f.ToTextValue())));
            }
        }

        static public void XGIDeviceWriteTest(XGTConnection conn, int addr)
        {
            //var conn = new XGTConnection(_XGK_IP, true);
            //bit WriteTest
            var lst = new List<XGTDevice>();
            //for (int i = 0; i < MAX_RANDOM_WRITE_POINTS; i++)
            lst.Add(new XGTDeviceBit('M', 11329) { Value = true });

            _ = conn.WriteRandomDevice(lst.ToArray());
        }
    }



}