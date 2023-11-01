using System;

using NetMQ;
using NetMQ.Sockets;

class ClientProgram
{
    static void Main()
    {
        Console.WriteLine("Client started.");

        using (var client = new DealerSocket())
        {
            client.Options.Identity = Guid.NewGuid().ToByteArray(); // 각 클라이언트에 대한 고유 식별자 설정

            client.Connect("tcp://localhost:5555");

            client.SendFrame("REGISTER"); // 여기서 주소를 첨부할 필요는 없습니다. DealerSocket이 자동으로 처리합니다.

            while (true)
            {
                var message = client.ReceiveFrameString();
                Console.WriteLine($"Received: {message}");
            }
        }
    }
}
