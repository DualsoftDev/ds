using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Server.Common;
using Server.Common.Kafka;
using Server.Common.NMC;

public class DsBridge
{
    private static KafkaProduce producer;
    private static KafkaConsume consumer;
    private static Dictionary<string, int> mapInput; // name, idx
    private static Dictionary<string, int> mapOutput; // name, idx
    private static Dictionary<string, short> valueInput; // name, value
    private static IBridgeHandler brdHnd;
    static void UpdateMessage(string content)
    {
        Console.WriteLine(content);
        var cmd = JObject.Parse(content);
        var from = cmd["from"].ToString();
        if (from != "ds-bridge")
        {
            var name    = cmd["name"].ToString();
            var targets = cmd["targets"];
            var btnType = cmd["button_type"].ToString();
            var onoff   = cmd["value"].ToString();
            if (mapOutput.ContainsKey(name))
            {
                var idx   = (short)mapOutput[name];
                var value = short.Parse(onoff);
                brdHnd.Transfer(idx, value);
            }
        }
    }

    static void Receiver(short[] input, string bridgeType)
    {
        foreach(var checker in mapInput)
        {
            var _name = checker.Key;
            var _idx = checker.Value;
            var nowValue = input[_idx];
            if (valueInput[_name] != nowValue)
            {
                var streamData = 
                    new { 
                        name = _name, 
                        value = nowValue, 
                        from = "ds-bridge" 
                    };
                producer.TransferData(
                    JObject.FromObject(streamData).ToString()
                );
                valueInput[_name] = nowValue;
            }
        }
    }

    static void UsingPaix(short addr, short numIO, JToken kafkaInfo)
    {
        var topic   = kafkaInfo["topic"].ToString();
        var kafkaIp = kafkaInfo["ip"].ToString();
        producer    = new KafkaProduce(topic, kafkaIp);
        consumer    = new KafkaConsume(topic, kafkaIp);
        brdHnd = new DsPaixHandler(addr, numIO);
        _ = Task.Run(() => { consumer.StreamConsume(UpdateMessage); });
        _ = Task.Run(() => { brdHnd.Receive(Receiver); });
    }

    static void GetMapIO()
    {
        mapInput   = new Dictionary<string, int>();
        mapOutput  = new Dictionary<string, int>();
        valueInput = new Dictionary<string, short>();
        foreach (var io in mapInput)
            valueInput[io.Key] = 0;
    }

    static void Bridge(JToken kafkaInfo, JToken bridgeInfo)
    {
        switch (bridgeInfo["type"].ToString())
        {
            case "paix":
                GetMapIO();
                var addr = bridgeInfo["ip"].ToString();
                var numIO = bridgeInfo["numIO"].ToString();
                UsingPaix(short.Parse(addr), short.Parse(numIO), kafkaInfo);
                break;
        }
    }

    static void StartUp(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var config = JObject.Parse(json);
        var kafkaInfo = config["kafka"];
        var bridgeInfo = config["bridge"];
        Bridge(kafkaInfo, bridgeInfo);
    }
    
    static void Main(string[] args)
    {
        StartUp("./config.json");
        Console.ReadKey();
    }
}