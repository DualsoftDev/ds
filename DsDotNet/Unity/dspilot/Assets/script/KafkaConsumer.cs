using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Random = UnityEngine.Random;
using System.Linq;

class KafkaConsumer : MonoBehaviour
{
    //static DSData dsData;

    public static Material material;
    public Material inputMaterial;

    public float time;
    private float timer = 0;

    //static bool isDicClear = false;

    [System.Serializable]
    public class threadHandle
    {
        // IConsumer<Ignore, string> c;
        ConsumerConfig consumer_config;

        const string _GroupId = "ds-info-group-2";
        const string _BootstrapServers = "192.168.0.194:9092";
        const bool _EnableAutoCommit = true;
        const bool _EnableAutoOffsetStore = true;
        const AutoOffsetReset _AutoOffsetReset = AutoOffsetReset.Latest;
        const string topic = "ds_engine";

        

        static private readonly ProducerConfig producerConfig = new()
        {
            Acks = 0,    //(Acks?)1,
            BootstrapServers = _BootstrapServers,
            ClientId = Dns.GetHostName()
        };

        static private readonly ConsumerConfig consumerConfig = new()
        {
            GroupId = _GroupId,
            BootstrapServers = _BootstrapServers,
            EnableAutoCommit = _EnableAutoCommit,
            EnableAutoOffsetStore = _EnableAutoOffsetStore,
            AutoOffsetReset = _AutoOffsetReset
        };



        //public readonly ConcurrentQueue<StreamMessage> _queue = new ConcurrentQueue<StreamMessage>();
        public void StartKafkaListener()
        {
            // for update

            Debug.Log("Kafka - Starting Thread..");
            try
            {
                Debug.Log("Kafka - Consumer");
                _ = Task.Run(() => { Consumer(topic, consumerConfig); });
                Debug.Log("wait");
                Thread.Sleep(5000);

                //Produce for start to init
                Debug.Log("Kafka - Produce");
                _ = Task.Run(async () => { await Produce(topic, producerConfig); });
                //Debug.Log("Kafka - Created config");
            }
            catch (Exception ex)
            {
                Debug.Log("Kafka - Received Expection: " + ex.Message + " trace: " + ex.StackTrace);
            }
        }
    }

    bool kafkaStarted = false;
    Thread kafkaThread;
    threadHandle _handle;

    void Start()
    {
        material = inputMaterial;
        StartKafkaThread();
    }



    void Update()
    {
        if (Input.GetKeyUp(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.C))
        {
            Debug.Log("Cancelling Kafka!");
            StopKafkaThread();
        }

        //ProcessKafkaMessage();
    }

    void OnDisable()
    {
        StopKafkaThread();
    }
    void OnApplicationQuit()
    {
        StopKafkaThread();
    }

    public void StartKafkaThread()
    {
        if (kafkaStarted) return;

        _handle = new threadHandle();
        kafkaThread = new Thread(_handle.StartKafkaListener);

        kafkaThread.Start();
        kafkaStarted = true;
    }

    
    static bool CheckOverlapMessage(JObject message)
    {
        return (string)message == DSData.prevMessage;
    }

    void StopKafkaThread()
    {
        if (kafkaStarted)
        {
            DSData.mode = DSData.stop;
            kafkaThread.Abort();
            kafkaThread.Join();
            kafkaStarted = false;
        }
    }

    static async Task Produce(string topic, ClientConfig config, string _mode = DSData.init)
    {
        await Task.Run(() =>
        {
            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
               
                var key = topic;

                var now = DateTime.Now.ToLocalTime();
                var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
                int timestamp = (int)span.TotalSeconds;

                var val = JObject.FromObject(new { mode = _mode, from = "DSPilot", timestamp = timestamp}).ToString(Formatting.None);

                Console.WriteLine($"Producing record: {key} {val}");

                producer.Produce(topic, new Message<string, string> { Key = key, Value = val },
                        (deliveryReport) =>
                        {
                            if (deliveryReport.Error.Code != ErrorCode.NoError)
                                Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");

                        });
                producer.Flush(TimeSpan.FromSeconds(1));
                DSData.mode = DSData.init;
            }
        }
        );
    }




    static private void Consumer(string topic, ClientConfig config)
    {
        using (var c = new ConsumerBuilder<string, string>(config).Build())
        {
                
            var topicPartition = new TopicPartition(topic, new Partition(0));
            var offset = c.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(1));
            var tpo = new TopicPartitionOffset(topicPartition, offset.High);
            c.Subscribe(topic);
            c.Assign(tpo);
            c.Seek(tpo);
                

            Debug.Log("Kafka - Subscribed");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            try
            {

                while (true)
                {                   
                    if ((DSData.mode == DSData.stop)) { break; }
                    try
                    {
                        //Debug.Log($"testing... {cts.Token}");
                            
                        var cr = c.Consume(cts.Token);  // Waiting for message

                        var jObj = JObject.Parse(cr.Message.Value);

                        //Debug.Log(jObj["mode"]);
                        if (jObj["from"].ToString() == "info_server")
                        {
                            //Debug.Log($"test {jObj["mode"]}");
                            //if((string)jObj["mode"] == DSData.init)
                            //Debug.Log($"{jObj["mode"]}  {jObj["timestamp"]}");

                            //Initialize
                            _ = Task.Run(() => { Initialize(jObj); });

                            //Streaming
                             Streaming(jObj);

                            //Update

                            //Infomation

                        }
                    }
                    catch (ConsumeException e)
                    {
                        Debug.Log("Kafka - Error occured: " + e.Error.Reason);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Kafka - Canceled..");
                // Ensure the consumer leaves the group cleanly and final offsets are committed.
                c.Close();
            }
        }
    }


    //public async Task GetInitReal(JObject message)
    static void Initialize(JObject message)
    {
        //Debug.Log("Initialize set");
        if (!(DSData.mode == DSData.init)) { return; }
        if (!((string)message["mode"] == DSData.init)) { return; }
        //if (CheckOverlapMessage(message)) { return; }  //같은 message면 return
        Debug.Log("Initialize set start"); 
        DSData.percent = 10;


        foreach (var real in message["reals"])
        {
            //Debug.Log("test reals");
            string realName = real["name"].ToString();
            Real myReal = new Real(realName, (string)real["parent"]);

            if (!DSData.realDic.ContainsKey(realName))
                DSData.realDic.Add(realName, myReal);
            
            foreach (var call in message["calls"])
            {
                //Debug.Log("test calls");
                if (realName == (string)call["parent"])
                {
                    Call child = new Call((string)call["name"], (string)call["parent"], (float)call["position"]["x"], (float)call["position"]["y"], (float)call["size"]["w"], (float)call["size"]["h"]);
                    //Debug.Log("call created" + child.x +":"+ child.y);
                    DSData.realDic[child.parent].children.Add(child.name, child);
                    if(!DSData.callDic.ContainsKey(child.name))
                        DSData.callDic.Add(child.name, child);
                    //Debug.Log("add child into real's chhild dict");
                    DSData.realDic[child.parent].indices.Add(child.name, (int)real["indices"][child.name]);
                    //Debug.Log("add child into real's indices dict");
                    //child.finishValue = 0.7f;
                }
            }
            
            foreach (Call call in DSData.realDic[realName].children.Values)
            {
                Debug.Log(call.name + " calculate finish value ");
                double value = 0.0;
                foreach (var callTarget in real["targets"][call.name])
                {
                    //Debug.Log("callTarget = "+callTarget.ToString());
                    var key = callTarget.ToString().Split(':')[0]; 
                    var val = callTarget.ToString().Replace($"{key}:", ""); 
                    //Debug.Log(key + " : " + val);
                    value += Math.Exp(int.Parse(real["indices"][call.name].ToString())) * (Convert.ToBoolean(val) ? 1 : 0); 
                }
                call.finishValue = (float)(Math.Log(value) / (DSData.realDic[realName].indices.Count + 1));    //pie 크기 보정
                //Debug.Log(call.name + " : " + call.finishValue);
            }


            Debug.Log("reals");
            foreach (var seg in message["reals"])
            {
                string segName = (string)seg["name"];
                string type = (string)seg["type"];
                string parent = (string)seg["parent"];
                string status = (string)seg["status"];


                DSData.Instance.segments.Add(segName, new DSData.Segments());
                DSData.Segments segment = DSData.Instance.segments[segName];
                segment.Type = type;
                segment.Parent = parent;
                segment.Status = status;


                   //Theta > stream

                segment.Indices = seg["indices"].ToObject<Dictionary<string, int>>();

                // 데이터 받기 수정 필요
                segment.Targets = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, bool>>>(seg["targets"].ToString());//seg["targets"].ToDictionary<string, Dictionary<string,bool>>();
                Debug.Log(segment.Targets);


            }

            Debug.Log("calls");
            foreach (var seg in message["calls"])
            {
                string segName = (string)seg["name"];
                string type = (string)seg["type"];
                string parent = (string)seg["parent"];
                string status = (string)seg["status"];
                Debug.Log(seg);
                Debug.Log($"    {segName}        {type}         {parent}    {status}   ");
                
                DSData.Instance.segments.Add(segName, new DSData.Segments()); Debug.Log("test01");
                DSData.Segments segment = DSData.Instance.segments[segName]; Debug.Log("test02");
                segment.Type = type;
                segment.Parent = parent;
                //segment.Status = status;

                
                float x = (float)seg["position"]["x"]; 
                float y = (float)seg["position"]["y"]; 
                float width = (float)seg["size"]["w"]; 
                float height = (float)seg["size"]["h"]; Debug.Log("test0");
                //Value

                segment.Position = new DSData.Position(x,y); Debug.Log("test1");
                segment.Size = new DSData.Size(width, height); Debug.Log("test2");
            }

                DSData.prevMessage = (string)message;
        }

       

        DSData.percent = 20;
        Debug.Log("Initialize done");
        DSData.needInitialize = true;
        //DSData.mode = DSData.stream;    //graphManager
    }

    /*
     			"targets": {
				"ctrl_sys.main_flow.R1.tsk.M1": {
					"ctrl_sys.main_flow.R1.tsk.P1": false,
					"ctrl_sys.main_flow.R1.tsk.M1": true,
					"ctrl_sys.main_flow.R1.tsk.P2": false,
					"ctrl_sys.main_flow.R1.tsk.M2": true
				},
				"ctrl_sys.main_flow.R1.tsk.M2": {
					"ctrl_sys.main_flow.R1.tsk.P1": true,
					"ctrl_sys.main_flow.R1.tsk.M1": false,
					"ctrl_sys.main_flow.R1.tsk.P2": false,
					"ctrl_sys.main_flow.R1.tsk.M2": true
				},
				"ctrl_sys.main_flow.R1.tsk.P1": {
					"ctrl_sys.main_flow.R1.tsk.P1": true,
					"ctrl_sys.main_flow.R1.tsk.M1": false,
					"ctrl_sys.main_flow.R1.tsk.P2": false,
					"ctrl_sys.main_flow.R1.tsk.M2": true
				},
				"ctrl_sys.main_flow.R1.tsk.P2": {
					"ctrl_sys.main_flow.R1.tsk.P1": true,
					"ctrl_sys.main_flow.R1.tsk.M1": false,
					"ctrl_sys.main_flow.R1.tsk.P2": true,
					"ctrl_sys.main_flow.R1.tsk.M2": false
				}
			}
     */



    static void Streaming(JObject message)
    {
        if (!(DSData.mode == DSData.stream)) { return; }
        if (!((string)message["mode"] == DSData.stream)) { return; }

        string name = message["name"].ToString();
        string parent = message["parent"].ToString();

        if((string)message["type"] == "real")
        {
            DSData.realDic[name].status = message["type"].ToString();
            DSData.realDic[name].theta = (float)message["threta"];
        }

        if ((string)message["type"] == "call")
        {
            DSData.realDic[parent].children[name].status = message["status"].ToString();
            //if(DSData.realDic[parent].children[name].status == DSData.finish)
                DSData.realDic[parent].theta = (float)message["threta"];
        }
        Debug.Log("streaming done");
    }
}

