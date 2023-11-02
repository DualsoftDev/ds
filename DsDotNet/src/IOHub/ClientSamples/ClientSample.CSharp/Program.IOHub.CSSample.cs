using IO.Core;
using static IO.Core.ZmqClient;

var port = 5555;
using var client = new Client($"tcp://localhost:{port}");

client.WriteBits("p/o", new[] { 1, 2, 3, 4 }, new[] { true, true, true, true });

bool[] results;
client.CsReadBits("p/o", new[] { 1, 2, 3, 4 })
    .Match(
        bits => { results = bits; return true; },
        err => false
    );
    
Console.WriteLine();
