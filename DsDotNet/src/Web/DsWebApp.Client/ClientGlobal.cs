//using IoHubClient = IO.Core.Client;

using Blazored.LocalStorage;

using DsWebApp.Shared;

using Dual.Common.Core;
using Dual.Web.Blazor.ClientSide;
using Dual.Web.Blazor.ServerSide;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using System.ComponentModel;
using System.Net.Http.Json;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

using static Engine.Core.HmiPackageModule;
using static System.Net.WebRequestMethods;

namespace DsWebApp.Client;

public class ClientGlobal : ClientGlobalBase, INotifyPropertyChanged
{
    CompositeDisposable _disposables = new();
    HubConnection _hubConnectionModel;
    public HMIPackage HmiPackage { get; set; }

    public Subject<TagWeb> TagChangedSubject = new Subject<TagWeb>();

    public ServerSettings ServerSettings { get; private set; }
    public DsClientSettings DsClientSettings => (DsClientSettings)base.ClientSettings;
    public string ModelDsZipPath { get; set; }
    public bool IsCpuRunning { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;

    public async Task InitializeAsync(HttpClient http, NavigationManager navigationManager, ILocalStorageService localStorage)
    {
        if (ServerSettings == null)
        {
            ServerSettings = await http.GetFromJsonAsync<ServerSettings>("api/serversettings");
            ModelDsZipPath = ServerSettings.RuntimeModelDsZipPath;
        }

        if (ServerSettings == null)
            Console.Error.WriteLine("Error: ServerSettings is null.");

        base.ClientSettings = await DsClientSettings.ReadAsync(localStorage);

        var result = await http.GetResultSimpleAsync<RuntimeModelDto>($"api/model");
        result.Iter(
            ok =>
            {
                ModelDsZipPath = ok.SourceDsZipPath;
                IsCpuRunning = ok.IsCpuRunning;
            },
            err => Console.Error.WriteLine($"Error: {err}"));


        _hubConnectionModel = await navigationManager.ToAbsoluteUri("/hub/model").StartHubAsync();

        IDisposable subscription =
            _hubConnectionModel.On<string>(SK.S2CNModelChanged, async modelDsZipPath =>
            {
                Console.WriteLine($"Model change detected on signalR: {modelDsZipPath}");
                ModelDsZipPath = modelDsZipPath;
                var result = await http.GetResultSimpleAsync<HMIPackage>($"api/hmi/package");
                result.Iter(
                    ok => HmiPackage = ok.Tee(pkg => pkg.BuildTagMap()),
                    err => Console.Error.WriteLine(err));
            });
        _disposables.Add(subscription);

        subscription =
            _hubConnectionModel.On<bool>(SK.S2CNCpuRunningStatusChanged, (bool isCpuRunning) =>
            {
                Console.WriteLine($"-------------- CPU RUNNING STATE CHANGED TO: {isCpuRunning}");
                IsCpuRunning = isCpuRunning;
            });
        _disposables.Add(subscription);
    }

    //RuntimeModelDto _modelDto;
    //HubConnection _hubConnectionModel;
    //public async Task<ResultSerializable<RuntimeModelDto, ErrorMessage>> GetModelDtoAsync(HttpClient http)
    //{
    //    if (_modelDto == null)
    //    {
    //        var result = await http.GetResultSimpleAsync<RuntimeModelDto>($"api/model");
    //        result.Iter(
    //            ok =>
    //            {
    //                _modelDto = ok;
    //                IsCpuRunning = _modelDto.IsCpuRunning;
    //            },
    //            err => Console.Error.WriteLine($"Error: {err}"));

    //        return result;
    //    }

    //    return ResultSerializable<RuntimeModelDto, ErrorMessage>.Ok(_modelDto);
    //}
    //public async Task<IDisposable> MonitorModelChangeAsync(NavigationManager navigationManager, HttpClient http, Action<RuntimeModelDto> onModelChanged)
    //{
    //    if (_hubConnectionModel == null)
    //        _hubConnectionModel = await navigationManager.ToAbsoluteUri("/hub/model").StartHubAsync();

    //    IDisposable subscription =
    //        _hubConnectionModel.On<string>(SK.S2CNModelChanged, async (modelDsZipPath) =>
    //        {
    //            ModelDsZipPath = modelDsZipPath;
    //            Console.WriteLine($"Model change detected on signalR: {modelDsZipPath}, {newModel.IsCpuRunning}");

    //            var result = await http.GetResultSimpleAsync<HMIPackage>($"api/hmi/package");
    //            result.Iter(
    //                ok => HmiPackage = ok.Tee(pkg => pkg.BuildTagMap()),
    //                err => Console.Error.WriteLine(err));

    //            _modelDto = newModel;
    //            onModelChanged(newModel);   // e.g StateHasChanged();
    //        });
    //    return subscription;
    //}


    static int _counter { get; set; } = 0;

    /* 직접 client 연결 불가.  browser 에서는 socket 지원 안됨 */
    //public ResettableLazy<IoHubClient> Client { get; private set; }
    public ClientGlobal()
    {
        Console.WriteLine("ClientGlobal ctor");
        if (_counter != 0)
            throw new InvalidOperationException("ClientGlobal must be singleton");
        _counter++;

        // todo:
        TagChangedSubject.Subscribe(tag =>
        {
            Console.WriteLine("In ClientGlobal.TagChangedSubject...");
            HmiPackage?.UpdateTag(tag);
        });

        //Client = new (() =>
        //{
        //    var client = new IoHubClient($"tcp://localhost:{5555}");
        //    var meta = client.GetMeta();

        //    return client;
        //});
    }
}
