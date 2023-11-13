using Newtonsoft.Json;

using IO.Core;
using static IO.Core.ZmqSpec;

namespace IO.WebServer.Demons;

public class Demon : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var specTxt = await File.ReadAllTextAsync("zmqsettings.json", stoppingToken);
        IOSpec ioSpec = JsonConvert.DeserializeObject<IOSpec>(specTxt);
        var server = new Server(ioSpec, stoppingToken);
        var serverThread = server.Run();
    }
}