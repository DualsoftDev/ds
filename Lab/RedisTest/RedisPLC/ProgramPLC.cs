using System;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace RedisProducer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var db = redis.GetDatabase();
            var sub = redis.GetSubscriber();

            // 메시지 수신 이벤트 핸들러 설정
            sub.Subscribe("g2d", (channel, message) =>
            {
                Console.WriteLine($" [x] Received from Consumer: {message}");
            });

            Console.WriteLine("Producer started. Type messages to send to Consumer. Type 'exit' to quit.");

            while (true)
            {
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                await sub.PublishAsync("d2g", input);
                Console.WriteLine($" [x] Sent to Consumer: {input}");
            }
        }
    }
}