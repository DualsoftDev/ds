using IO.Core;
using Newtonsoft.Json.Linq;

using static IO.Core.ZmqClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

var port = 5555;
using var client = new Client($"tcp://localhost:{port}");

var cts = new CancellationTokenSource();
client.TagChangedSubject.Subscribe(tag =>
{
    Console.WriteLine($"Total {tag.Offsets.Length} tag changed on {tag.Path}");
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
    var result = client.CsSendRequest(key);
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
