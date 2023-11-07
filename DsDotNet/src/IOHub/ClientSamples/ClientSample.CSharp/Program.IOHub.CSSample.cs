using IO.Core;

using NetMQ.Sockets;

using Newtonsoft.Json.Linq;
using System.IO;

var port = 5555;
using var client = new CSharpClient($"tcp://localhost:{port}");

var cts = new CancellationTokenSource();
client.TagChangedSubject.Subscribe(change =>
{
    Console.WriteLine($"Total {change.Offsets.Length} tag changed on {change.Path} with bitLength={change.ContentBitLength}");
    //foreach (var (offset, value) in change.Offsets.Zip(change.Values))
    var offsets = change.Offsets;
    switch (change.ContentBitLength)
    {
        case 1:
            {
                var values = change.Values as bool[];
                foreach (var (offset, value) in change.Offsets.Zip(values))
                    Console.WriteLine($"  {offset}: {value}");
                break;
            }
        case 8:
            {
                var values = change.Values as byte[];
                foreach (var (offset, value) in change.Offsets.Zip(values))
                    Console.WriteLine($"  {offset}: {value}");
                break;
            }
        case 16:
            {
                var values = change.Values as ushort[];
                foreach (var (offset, value) in change.Offsets.Zip(values))
                    Console.WriteLine($"  {offset}: {value}");
                break;
            }
        case 32:
            {
                var values = change.Values as uint[];
                foreach (var (offset, value) in change.Offsets.Zip(values))
                    Console.WriteLine($"  {offset}: {value}");
                break;
            }
        case 64:
            {
                var values = change.Values as ulong[];
                foreach (var (offset, value) in change.Offsets.Zip(values))
                    Console.WriteLine($"  {offset}: {value}");
                break;
            }
        default:
            throw new InvalidDataException($"Invalid bit length: {change.ContentBitLength}");
    }
});


Console.CancelKeyPress += (s, args) => {
    Console.WriteLine("Ctrl+C pressed!");
    cts.Cancel();
    args.Cancel = true; // 프로그램을 종료하지 않도록 설정 (선택 사항)
};


var key = "";
while ( key != null && ! cts.IsCancellationRequested )
{
    key = Console.ReadLine();
    if (key == null)
        continue;
    Console.WriteLine($"Got key [{key}].");
    if (key == "q" || key == "Q")
        break;
    var result = client.SendRequest(key);
    result.Match(
        value => { Console.WriteLine($"OK: {value}"); return true; },
        err => { Console.WriteLine($"ERR: {err}"); return false; }
        );
}

//var _ = Task.Factory.StartNew(() =>
//    {
//        client.WriteBits("p/o", new[] { 1, 2, 3, 4 }, new[] { true, true, true, true });

//        bool[] results;
//        client.CsReadBits("p/o", new[] { 1, 2, 3, 4 })
//            .Match(
//                bits => { results = bits; return true; },
//                err => false
//            );
//        while (true)
//            Thread.Sleep(500);

//    }, TaskCreationOptions.LongRunning);


//Thread.Sleep(Timeout.Infinite);
Console.ReadLine();
Console.WriteLine();


void DoNothign()
{
    var serverSocket = new RouterSocket();
}
