using System.Net;
using Confluent.Kafka;

namespace Server.Common.Kafka;

public class KafkaCommonOption
{
    protected string? Topic;
    protected string? ServerAddress;
    protected int? PartitionNumber;
    protected ProducerConfig? ProducerConf;
    protected ConsumerConfig? ConsumerConf;
}

public class KafkaProduce : KafkaCommonOption
{
    private ProducerConfig GenProducerConfig()
    {
        if (ServerAddress == null)
            throw new ArgumentNullException(ServerAddress);

        return new ProducerConfig
        {
            Acks = 0,
            BootstrapServers = ServerAddress,
            ClientId = Dns.GetHostName()
        };
    }

    public KafkaProduce(string topic, string serverAddress, int partitionNumber = 0)
    {
        Topic = topic;
        ServerAddress = serverAddress;
        ProducerConf = GenProducerConfig();
        PartitionNumber = partitionNumber;
    }

    public void TransferData(string streamData)
    {
        if (Topic == null)
            throw new ArgumentNullException(Topic);

        try
        {
            using (var producer = new ProducerBuilder<Null, string>(ProducerConf).Build())
            {
                producer.Produce(
                    topicPartition:
                        new TopicPartition(
                            Topic, 
                            new Partition(PartitionNumber.GetValueOrDefault())
                        ),
                    message: 
                        new Message<Null, string> { 
                            Value = streamData 
                        },
                    deliveryHandler: 
                        (deliveryReport) => {
                            if (deliveryReport.Error.Code != ErrorCode.NoError)
                                Console.WriteLine(
                                    $"Failed to deliver message : " +
                                    $"{deliveryReport.Error.Reason}" + $", " +
                                    $"tp : {deliveryReport.TopicPartition}"
                                );
                        }
                );
                producer.Flush(TimeSpan.FromSeconds(1));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(
                $"Exception has raised when " +
                $"[Transfer stream data - {streamData}] : \n" + 
                e
            );
        }
    }
}

public class KafkaConsume : KafkaCommonOption
{
    private ConsumerConfig GenConsumerConfig()
    {
        return new ConsumerConfig
        {
            GroupId = Guid.NewGuid().ToString(),
            BootstrapServers = ServerAddress
        };
    }

    public KafkaConsume(string topic, string serverAddress, int partitionNumber = 0)
    {
        Topic = topic;
        ServerAddress = serverAddress;
        ConsumerConf = GenConsumerConfig();
        PartitionNumber = partitionNumber;
    }

    public void StreamConsume(Action<string> receiver)
    {
        if (Topic == null)
            throw new ArgumentNullException(Topic);

        TopicPartition topicPartition = new (
            Topic, 
            new Partition(PartitionNumber.GetValueOrDefault())
        );
        CancellationTokenSource cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true; // prevent the process from terminating.
            cts.Cancel();
        };

        using (var consumer = new ConsumerBuilder<string, string>(ConsumerConf).Build())
        {
            consumer.Subscribe(Topic);
            try
            {
                var offset = 
                        consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));
                Console.WriteLine(
                    $"End of offset : {offset.High} in " +
                    $"{ServerAddress}({PartitionNumber})"
                );
                consumer.Assign(new TopicPartitionOffset(topicPartition, offset.High));
                //consumer.Seek(new TopicPartitionOffset(topicPartition, offset.High));
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Exception has raised when " +
                    "[Seek to the end of partition offset] : \n" + 
                    e
                );
            }

            try
            {
                while (true)
                {
                    var cr = consumer.Consume(cts.Token);
                    if (cr.Message.Value == null)
                        continue;

                    receiver(cr.Message.Value);
                }
            }
            catch (OperationCanceledException)
            {
                // ctrl + c
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"Exception has rised when " +
                    $"[Consume the messeages in {ServerAddress}({PartitionNumber})]: " + 
                    e
                );
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}