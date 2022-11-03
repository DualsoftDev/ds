using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Common.Kafka;
using Engine.CodeGen;

namespace Server.Information;

internal class InformationServer
{
    public Dictionary<ProducerType, Dictionary<string, KafkaProduce>> producers;
    public Dictionary<string, KafkaConsume> consumers;
    public InformationServer(string path)
    {
        // parsing server configurations
        var serverCfg = JObject.Parse(File.ReadAllText(path));
        producers = GenProducers(serverCfg);
        consumers = GenConsumers(serverCfg);
    }

    public enum ProducerType
    {
        none,
        distributor,
        information,
        broadcaster,
        heart_bit
    }

    private Dictionary<ProducerType, Dictionary<string, KafkaProduce>> GenProducers(JObject serverCfg)
    {
        var producers = new Dictionary<ProducerType, Dictionary<string, KafkaProduce>>();
        var addresses = serverCfg["addresses"];
        if (addresses == null)
            throw new ArgumentNullException("addresses");

        var produce = serverCfg["produce"];
        if (produce != null && produce.Count() > 0)
        {
            try
            {
                var topics = JObject.Parse(produce.ToString());
                foreach (var topic in topics)
                {
                    if (topic.Value == null || topic.Value.Count() == 0)
                        continue;
                    foreach (var prd in topic.Value)
                    {
                        var prodInfo = JObject.Parse(prd.ToString());
                        var typeStr = prodInfo["type"]!.ToString();
                        var pType = (ProducerType)Enum.Parse(typeof(ProducerType), typeStr);
                        if (!producers.ContainsKey(pType))
                            producers.Add(pType, new Dictionary<string, KafkaProduce>());
                        var partition = int.Parse(prodInfo["partition"]!.ToString());
                        var target = int.Parse(prodInfo["target"]!.ToString());
                        producers[pType].Add(
                            topic.Key,
                            new KafkaProduce(
                                topic.Key,
                                addresses[target]!.ToString(),
                                partition
                            )
                        );
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Exception has raised when " +
                    $"Parse producer config : \n" +
                    e
                );
            }
        }
        return producers;
    }

    private Dictionary<string, KafkaConsume> GenConsumers(JObject serverCfg)
    {
        var consumers = new Dictionary<string, KafkaConsume>();
        var addresses = serverCfg["addresses"];
        if (addresses == null)
            throw new ArgumentNullException("addresses");

        var consume = serverCfg["consume"];
        if (consume != null && consume.Count() > 0)
        {
            try
            {
                var topics = JObject.Parse(consume.ToString());
                foreach (var topic in topics)
                {
                    if (topic.Value == null)
                        continue;
                    var partition = 
                        int.Parse(topic.Value["partition"]!.ToString());
                    var target = 
                        int.Parse(topic.Value["target"]!.ToString());
                    consumers.Add(
                        topic.Key, 
                        new KafkaConsume(
                            topic.Key, 
                            addresses[target]!.ToString(), 
                            partition
                        )
                    );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Exception has raised when " +
                    $"Parse consumer config : \n" +
                    e
                );
            }
        }
        return consumers;
    }

    private void ModelHandler(string content)
    {
        // input : Ds model text
        // 1. saving content to db as a ds model
        //   + check differences of ds model if is possible
        // 2. parse ds model and generate CPU-HMI-PILOT codes
        // 3. distribute the codes and store the log to db
        var pm = new CodeGenHandler.ParseModel(content);
        if (pm != null)
        {
            var cCode = pm.CpuCode;
            var hCode = pm.HmiCode;
            var pCode = pm.PilotCode;
            Console.WriteLine(pCode);
        }
    }

    private void InformationHandler(string content)
    {
        // input : request for getting daily & monthly information
        // 1. getting day & month data from db
        // 2. generate information
        // 3. return to Ds pilot
    }

    private void StreamHandler(string content)
    {
        // input : stream data from control server
        // 1. collect stream data
        // 2. broadcast to the ds pilot when getting a request
    }

    private void MessageHandler(string eventString)
    {
        // input : every inputs on kafka brokers
        // format :
        //     {
        //         "mode": string,
        //         "from": string,
        //         "container": string, // maybe ds model or something else..
        //         "timestamp": datetime(YYYY.MM.DD_HH:MM:SS.sss)
        //     }
        ProducerType TypeSelector(string mode) =>
            mode switch
            {
                // upload from model editor ->
                //     distribute to DsPilot, HMI, constrol server, engine cpu
                "upload"      => ProducerType.distributor,

                // request from model editor ->
                //     upload stored ds model into kafk broker for model editor
                "download"    => ProducerType.distributor,

                // request from DsPilot, HMI, constrol server, engine cpu
                //     distribute to DsPilot, HMI, constrol server, engine cpu
                "initialize" => ProducerType.distributor,

                // request from DsPilot ->
                //     generate daily & monthly information and return
                "information" => ProducerType.information,

                // request from DsPilot ->
                //     get stream data from control server and
                //     streaming realtime data to the DsPilot
                "streaming"   => ProducerType.broadcaster,

                // mode error ->
                //     notify error and mode list with usage
                _             => ProducerType.none
            };

        var content = JObject.Parse(eventString);
        var targetType = TypeSelector(content["mode"]!.ToString());
        
    }

    public void Executor()
    {
        // start consumers
        foreach (var consumer in consumers)
            _ = Task.Run(() => {
                    consumer.Value.StreamConsume(MessageHandler);
                }
            );
    }
}