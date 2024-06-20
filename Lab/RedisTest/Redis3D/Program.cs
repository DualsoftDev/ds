using System;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace RedisConsumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var db = redis.GetDatabase();
            var sub = redis.GetSubscriber();

            // 메시지 수신 이벤트 핸들러 설정
            sub.Subscribe("toConsumer", (channel, message) =>
            {
                Console.WriteLine($" [x] Received from Producer: {message}");
            });

            Console.WriteLine("Consumer started. Type messages to send to Producer. Type 'exit' to quit.");

            while (true)
            {
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                await sub.PublishAsync("toProducer", input);
                Console.WriteLine($" [x] Sent to Producer: {input}");
            }
        }
    }
}