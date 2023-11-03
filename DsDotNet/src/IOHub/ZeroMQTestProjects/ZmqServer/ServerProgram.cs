using System.Reactive.Linq;

using NetMQ;
using NetMQ.Sockets;

class ServerProgram
{
    static void Main()
    {
        Console.WriteLine("Server started.");

        var clients = new List<byte[]>();  // byte[] 리스트로 변경

        using (var server = new RouterSocket())
        {
            server.Bind("tcp://*:5555");

            var thread =
                new Thread(() =>
                {
                    while (true)
                    {
                        server.TryReceiveMultipartMessage
                        var message = server.ReceiveMultipartMessage();
                        var clientAddress = message[0].Buffer;  // byte[]로 받음
                        var clientMessage = message[1].ConvertToString();

                        if (clientMessage == "REGISTER")
                        {
                            Console.WriteLine("Server: detected client registration.");
                            clients.Add(clientAddress);
                        }
                    }
                }) { IsBackground = true }
                ;
            thread.Start(); // 백그라운드 스레드로 설정

            Observable.Interval(TimeSpan.FromSeconds(3))
                .Subscribe(counter =>
                {
                    foreach (var client in clients)
                    {
                        Console.WriteLine("Server: sending to client new time");
                        server.SendMoreFrame(client).SendFrame(DateTime.Now.ToString());
                    }
                });

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
