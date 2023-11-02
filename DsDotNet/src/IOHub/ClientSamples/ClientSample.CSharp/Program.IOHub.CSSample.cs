using IO.Core;
using static IO.Core.ZmqClient;

var port = 5555;
using var client = new Client($"tcp://192.168.9.2:{port}");

client.TagChangedSubject.Subscribe(tag =>
{
    Console.WriteLine($"Total {tag.Offsets.Length} tag changed on {tag.Path}");
});

var _ = Task.Factory.StartNew(() =>
    {
        client.WriteBits("p/o", new[] { 1, 2, 3, 4 }, new[] { true, true, true, true });

        bool[] results;
        client.CsReadBits("p/o", new[] { 1, 2, 3, 4 })
            .Match(
                bits => { results = bits; return true; },
                err => false
            );
        while (true)
            Thread.Sleep(500);

    }, TaskCreationOptions.LongRunning);


Thread.Sleep(Timeout.Infinite);
Console.ReadLine();
Console.WriteLine();
