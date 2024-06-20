// Producer.cs

using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RmqCommon;

namespace Producer
{
    class ProgramPLC
    {
        static async Task Main(string[] args)
        {
            using var connection = RabbitMQHelper.GetConnection("localhost");
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "toConsumer", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: "toProducer", durable: false, exclusive: false, autoDelete: false, arguments: null);

            RabbitMQHelper.ReceiveMessages(channel, "toProducer", message =>
            {
                Console.WriteLine($" [x] Received from Consumer: {message}");
            });

            Console.WriteLine("Producer started. Type messages to send to Consumer. Type 'exit' to quit.");

            while (true)
            {
                var input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                RabbitMQHelper.SendMessage(channel, "toConsumer", input);
            }
        }
    }
}
