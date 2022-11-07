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
    public enum ProduceType
    {
        none,
        distributor,
        information,
        broadcaster,
        heart_bit
    }

    public class Response
    {
        public bool Succeed;
        public ProduceType Process;
        public object Body;
        public Response(bool succeed, ProduceType process, object body)
        {
            Succeed = succeed;
            Process = process;
            Body = body;
        }
    }

    public Dictionary<ProduceType, Dictionary<string, KafkaProduce>> producers;
    public Dictionary<string, KafkaConsume> consumers;
    public InformationServer(string path)
    {
        // parsing server configurations
        var serverCfg = JObject.Parse(File.ReadAllText(path));
        producers = GenProducers(serverCfg);
        consumers = GenConsumers(serverCfg);
    }

    private Dictionary<ProduceType, Dictionary<string, KafkaProduce>> 
        GenProducers(JObject serverCfg)
    {
        var producers = 
            new Dictionary<ProduceType, Dictionary<string, KafkaProduce>>();
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
                        var pType = 
                            (ProduceType)Enum.Parse(
                                typeof(ProduceType), 
                                typeStr
                            );
                        if (!producers.ContainsKey(pType))
                            producers.Add(
                                pType, 
                                new Dictionary<string, KafkaProduce>()
                            );
                        var partition = 
                            int.Parse(prodInfo["partition"]!.ToString());
                        var target = 
                            int.Parse(prodInfo["target"]!.ToString());
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

    private void DbHandler(Response resp)
    {
        //
        Console.WriteLine(
            $"succeed : {resp.Succeed}\n" +
            $"process : {resp.Process}\n" +
            $"body : {resp.Body}"
        );
    }

    private Response ModelUploadHandler(ProduceType mode, JToken container)
    {
        // input : Ds model text
        // 1. store container into DB
        //   + check differences of ds model if is possible
        // 2. parse ds model and generate CPU-HMI-PILOT codes
        // 3. distribute the codes
        try
        {
            var resultList = new List<CodeGen.Initializer>();
            var pm = 
                new CodeGenHandler.ParseModel(container["body"]!.ToString());
            if (pm != null)
            {
                resultList.Add(pm.CpuResult);
                resultList.Add(pm.PilotResult);
                resultList.Add(pm.HmiResult);
                foreach (var code in resultList)
                {
                    var res = code.succeed ? code.body : code.error;
                    var returner =
                        JsonConvert.SerializeObject(
                            new {
                                mode = "init",
                                from = "info-server",
                                initializer = res
                            }
                        );
                    _ = Task.Run(() => {
                            producers[mode][code.from].TransferData(returner!);
                        }
                    );
                }
            }

            return new Response(true, mode, resultList);
        }
        catch (Exception e)
        {
            var target = "model-input";
            var returner =
                JsonConvert.SerializeObject(
                    new {
                        mode = "init",
                        from = "info-server",
                        initializer = e.Message
                    }
                );
            _ = Task.Run(() => {
                    producers[mode][target].TransferData(returner!);
                }
            );
            return new Response(false, mode, e.Message);
        }
    }

    private Response ModelDownloadHandler(ProduceType mode, JToken container)
    {
        // input : ds model get request
        // 1. getting ds model from DB
        // 2. produce to target topic
        try
        {
            return new Response(true, mode, "");
        }
        catch (Exception e)
        {
            return new Response(false, mode, e.Message);
        }
    }

    private Response ModelInitializeHandler(ProduceType mode, JToken container)
    {
        // input : initialize request
        // 1. getting ds model from DB
        // 2. change ds model to target code
        // 3. produce to target topic
        try
        {
            var model = new { }; 
            var pm = 
                new CodeGenHandler.ParseModel(model.ToString());
            var target = container["from"]!.ToString();
            var genRes = pm.SelectedResult(target);
            var res = genRes.succeed ? genRes.body : genRes.error;
            var returner =
                JsonConvert.SerializeObject(
                    new {
                        mode = "init",
                        from = "info-server",
                        initializer = res
                    }
                );
            _ = Task.Run(() => {
                    producers[mode][target].TransferData(returner);
                }
            );
            return new Response(true, mode, genRes);
        }
        catch (Exception e)
        {
            var target = container["from"]!.ToString();
            var returner =
                JsonConvert.SerializeObject(
                    new {
                        mode = "init",
                        from = "info-server",
                        initializer = e.Message
                    }
                );
            _ = Task.Run(() => {
                    producers[mode][target].TransferData(returner);
                }
            );
            return new Response(false, mode, e.Message);
        }
    }
    
    private Response InformationHandler(ProduceType mode, JToken container)
    {
        // input : process for getting daily & monthly information
        // 1. getting day & month data from db
        // 2. generate information
        // 3. produce to Ds pilot
        try
        {
            return new Response(true, mode, "");
        }
        catch (Exception e)
        {
            return new Response(false, mode, e.Message);
        }
    }
    
    private Response StreamHandler(ProduceType mode, JToken container)
    {
        // input : stream data from control server
        // 1. collect stream data
        // 2. broadcast to the ds pilot when getting a request
        try
        {
            return new Response(true, mode, "");
        }
        catch (Exception e)
        {
            return new Response(false, mode, e.Message);
        }
    }
    
    private void MessageHandler(string eventString)
    {
        // input : every inputs on kafka brokers
        // format :
        //     {
        //         "container": {
        //             "from": string,
        //             "body": object,
        //             "timestamp": datetime(YYYY.MM.DD_HH:MM:SS.sss)
        //         },
        //         "mode": string
        //     }
        Response TypeSelector(string mode, JToken container) =>
            mode switch
            {
                // upload from model editor ->
                //     distribute to DsPilot, HMI, constrol server, engine cpu
                "upload" =>
                    ModelUploadHandler(ProduceType.distributor, container),
                    
                // request from model editor ->
                //     upload stored ds model into kafk broker for model editor
                "download" =>
                    ModelDownloadHandler(ProduceType.distributor, container),

                // request from DsPilot, HMI, constrol server, engine cpu
                //     return a parsed ds code
                "initialize" =>
                    ModelInitializeHandler(ProduceType.distributor, container),

                // request from DsPilot ->
                //     generate daily & monthly information and return
                "information" =>
                    InformationHandler(ProduceType.information, container),

                // request from DsPilot ->
                //     get stream data from control server and
                //     streaming realtime data to the DsPilot
                "streaming" =>
                    StreamHandler(ProduceType.broadcaster, container),

                // mode error ->
                //     notify error and mode list with usage
                _ =>
                    new Response(false, ProduceType.none, "type error")
            };

        var content = JObject.Parse(eventString);
        var mode = content["mode"]!.ToString();
        var container = content["container"]!;
        if (container != null && container["from"]!.ToString() != "info-server")
        {
            var response = TypeSelector(mode, container);
            DbHandler(response);
        }
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