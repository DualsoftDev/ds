// Consumer.cs
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RmqCommon;

namespace Consumer
{
    class Program3D
    {
        static async Task Main(string[] args)
        {
            using var connection = RabbitMQHelper.GetConnection("localhost");
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "toConsumer", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: "toProducer", durable: false, exclusive: false, autoDelete: false, arguments: null);

            RabbitMQHelper.ReceiveMessages(channel, "toConsumer", message =>
            {
                Console.WriteLine($" [x] Received from Producer: {message}");
            });

            Console.WriteLine("Consumer started. Type messages to send to Producer. Type 'exit' to quit.");

            while (true)
            {
                var input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                RabbitMQHelper.SendMessage(channel, "toProducer", input);
            }
        }
    }
}
