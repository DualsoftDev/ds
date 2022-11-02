using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Common.Kafka;

namespace Server.Control;

internal class Startup
{
    static void PrintMessage(string content)
    {
        Console.WriteLine(content);
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Kafka basic tester");
        var ProdToLocal = new KafkaProduce("tester", "localhost:9092", 0);
        var ProdToOther = new KafkaProduce("tester", "192.168.0.201:9092", 1);
        var ConsumeFromLocal = new KafkaConsume("tester", "localhost:9092", 0);
        var ConsumeFromOther = new KafkaConsume("tester", "192.168.0.201:9092", 1);

        var streamData1 = new { content = "hello" };
        var streamData2 = new { content = "hi" };

        _ = Task.Run(() => { ConsumeFromLocal.StreamConsume(PrintMessage); });
        _ = Task.Run(() => { ConsumeFromOther.StreamConsume(PrintMessage); });
        _ = Task.Run(() => {
            Thread.Sleep(1000);
            for (int i = 0; i < 10; i++)
            {
                ProdToLocal.TransferData(JObject.FromObject(streamData1).ToString(Formatting.None));
                ProdToOther.TransferData(JObject.FromObject(streamData2).ToString(Formatting.None));
            }
        });
        Console.ReadKey();
    }
}