using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Server.Common.Kafka;

using Dsu.PLC.Common;
using Dsu.PLC.LS;
using Microsoft.FSharp.Core;
using System.IO;
using Engine.Common;
using System.Reflection;

public class Startup
{
    static void PrintMessage(string content)
    {
        Console.WriteLine(content);
    }

    static void Main(string[] args)
    {
        var conn = new LsConnection(new LsConnectionParameters("192.168.0.101", new FSharpOption<ushort>(2004), TransportProtocol.Tcp, 3000.0));
        var ProdToLocal = new KafkaProduce("ds-test", "192.168.0.27:9092");
        var ConsumeFromLocal = new KafkaConsume("ds-test", "192.168.0.27:9092");
        //string text = File.ReadAllText(@"E:\test1.ds");
        var streamData = new { response = new { from = "hmi-tester", status = true, body = "hi", timestamp = "" }, mode = "stream" };
        conn.PerRequestDelay = 1000;
        if (conn.Connect())
        _ = Task.Run(() => { ConsumeFromLocal.StreamConsume(PrintMessage); });
        _ = Task.Run(() => { ProdToLocal.TransferData(JObject.FromObject(streamData).ToString()); });

        Console.ReadKey();
    }
}