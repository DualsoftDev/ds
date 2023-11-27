//using IoHubClient = IO.Core.Client;

using DsWebApp.Shared;

using Dual.Common.Core;
using Dual.Web.Blazor.ClientSide;
using Dual.Web.Blazor.ServerSide;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using System.Net.Http.Json;

using static Engine.Core.HmiPackageModule;

namespace DsWebApp.Client;

public class ClientGlobal : ClientGlobalBase
{
    public HMIPackage HmiPackage { get; set; }

    ServerSettings _serverSettings;
    public async Task<ServerSettings> GetServerSettingsAsync(HttpClient http)
    {
        if (_serverSettings == null)
            _serverSettings = await http.GetFromJsonAsync<ServerSettings>("api/serversettings");

        return _serverSettings;
    }

    RuntimeModelDto _modelDto;
    HubConnection _hubConnectionModel;
    public async Task<ResultSerializable<RuntimeModelDto, ErrorMessage>> GetModelDtoAsync(HttpClient http)
    {
        await Console.Out.WriteLineAsync("[1]");
        if (_modelDto == null)
        {
            await Console.Out.WriteLineAsync("[2]");
            return await http.GetResultSimpleAsync<RuntimeModelDto>($"api/model");
        }

        return ResultSerializable<RuntimeModelDto, ErrorMessage>.Ok(_modelDto);
    }
    public async Task<IDisposable> MonitorModelChangeAsync(NavigationManager navigationManager, Action<RuntimeModelDto> onModelChanged)
    {
        if (_hubConnectionModel == null)
            _hubConnectionModel = await navigationManager.ToAbsoluteUri("/hub/model").StartHubAsync();

        IDisposable subscription =
            _hubConnectionModel.On<RuntimeModelDto>(SK.S2CNModelChanged, (RuntimeModelDto newModel) =>
            {
                Console.WriteLine($"Model change detected on signalR: {newModel.SourceDsZipPath}, {newModel.IsCpuRunning}");
                _modelDto = newModel;
                onModelChanged(newModel);   // e.g StateHasChanged();
            });
        return subscription;
    }


    static int _counter { get; set; } = 0;

    /* 직접 client 연결 불가.  browser 에서는 socket 지원 안됨 */
    //public ResettableLazy<IoHubClient> Client { get; private set; }
    public ClientGlobal()
    {
        Console.WriteLine("ClientGlobal ctor");
        if (_counter != 0)
            throw new InvalidOperationException("ClientGlobal must be singleton");
        _counter++;

        //Client = new (() =>
        //{
        //    var client = new IoHubClient($"tcp://localhost:{5555}");
        //    var meta = client.GetMeta();

        //    return client;
        //});
    }
}
