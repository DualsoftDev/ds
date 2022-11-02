using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Common.Kafka;

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
        distributor,
        information,
        broadcaster,
        heart_bit
    }

    public Dictionary<ProducerType, Dictionary<string, KafkaProduce>> GenProducers(JObject serverCfg)
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
                        var typeStr = prodInfo["type"].ToString();
                        var pType = (ProducerType)Enum.Parse(typeof(ProducerType), typeStr);
                        if (!producers.ContainsKey(pType))
                            producers.Add(pType, new Dictionary<string, KafkaProduce>());
                        var partition = int.Parse(prodInfo["partition"].ToString());
                        var target = int.Parse(prodInfo["target"].ToString());
                        producers[pType].Add(
                            topic.Key,
                            new KafkaProduce(
                                topic.Key,
                                addresses[target].ToString(),
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

    public Dictionary<string, KafkaConsume> GenConsumers(JObject serverCfg)
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
                    var partition = int.Parse(topic.Value["partition"].ToString());
                    var target = int.Parse(topic.Value["target"].ToString());
                    consumers.Add(
                        topic.Key, 
                        new KafkaConsume(
                            topic.Key, 
                            addresses[target].ToString(), 
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

    public void ModelHandler(string content)
    {

    }

    public void InformationHandler(string content)
    {

    }

    public void RequestHandler(string content)
    {

    }

    public void MessageHandler(string content)
    {
        Console.WriteLine(content);

    }

    public void Executor()
    {
        // start consumers
        foreach (var consumer in consumers)
            _ = Task.Run(() => {consumer.Value.StreamConsume(MessageHandler);});
    }
}