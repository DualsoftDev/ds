using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Server.Common.Kafka;
using Microsoft.FSharp.Core;
using Dsu.PLC.LS;
using Dsu.PLC.Common;
using System.Reactive.Linq;

public class ServerTester
{
    static void PrintMessage(string content)
    {
        Console.WriteLine(content);
    }

    static async Task CommunicationPLC(LsConnection conn, LsTag[] writableTags, List<LsTag> readableTags)
    {
        if (conn.Connect())
        {
            conn.WriteRandomTags(writableTags);
            conn.AddMonitoringTags(readableTags);
            conn.Subject
                .OfType<TagValueChangedEvent>()
                .Subscribe(evt =>
                    {
                        var tag = (LsTag)evt.Tag;
                        if ((bool)tag.Value)
                            Console.WriteLine("on");
                        else
                            Console.WriteLine("off");
                    }
                );
            await conn.StartDataExchangeLoopAsync();
        }
    }

    static void Main(string[] args)
    {
        var ProdToLocal = new KafkaProduce("ds-test", "192.168.0.27:9092");
        var ConsumeFromLocal = new KafkaConsume("ds-test", "192.168.0.27:9092");
        var streamData = new { response = new { from = "hmi-tester", status = true, body = "hi", timestamp = "" }, mode = "stream" };
        var conn = new LsConnection(new LsConnectionParameters("192.168.0.101", new FSharpOption<ushort>(2004), TransportProtocol.Tcp, 3000.0));
        conn.PerRequestDelay = 1000;
        var bitP1 = (LsTag)conn.CreateTag("P01000");
        var bitP2 = (LsTag)conn.CreateTag("P02000");
        bitP1.Value = false;
        bitP2.Value = false;
        var writableTags = new List<LsTag>() { bitP1 }.ToArray();
        var readableTags = new List<LsTag>() { bitP2 };
        _ = Task.Run(async () => { await CommunicationPLC(conn, writableTags, readableTags); });
        _ = Task.Run(() => { ConsumeFromLocal.StreamConsume(PrintMessage); });
        _ = Task.Run(() => { 
            Thread.Sleep(1000); 
            ProdToLocal.TransferData(JObject.FromObject(streamData).ToString());
        });
        Console.ReadKey();
    }
}