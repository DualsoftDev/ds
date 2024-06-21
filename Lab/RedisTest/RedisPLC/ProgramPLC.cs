using System;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace RedisProducer
{
    class Program
    {
        // 구독할 채널 및 발행할 채널 설정
        static string subscribeChannel = "g2d";
        static string publishChannel = "d2g";

        static async Task Main(string[] args)
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var db = redis.GetDatabase();
            var sub = redis.GetSubscriber();

            // 메시지 수신 이벤트 핸들러 설정
            sub.Subscribe(subscribeChannel, (channel, message) =>
            {
                Console.WriteLine($" [x] Received from Graphic: {message}");
            });

            Console.WriteLine("DS started. Type messages to send to Graphic. Type 'exit' to quit.");

            while (true)
            {
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                await sub.PublishAsync(publishChannel, input);
                Console.WriteLine($" [x] Sent to Graphic: {input}");
            }
        }
    }
}