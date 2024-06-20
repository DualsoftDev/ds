using System.Text;
// RabbitMQHelper.cs
using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RmqCommon
{

    public static class RabbitMQHelper
    {
        public static IConnection GetConnection(string hostname)
        {
            var factory = new ConnectionFactory() { HostName = hostname };
            return factory.CreateConnection();
        }

        public static void SendMessage(IModel channel, string queueName, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
            Console.WriteLine($" [x] Sent to {queueName}: {message}");
        }

        public static void ReceiveMessages(IModel channel, string queueName, Action<string> onMessageReceived)
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                onMessageReceived(message);
            };
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }
    }
}
