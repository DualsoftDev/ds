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

    HttpClient _http;
    NavigationManager _navigationManager;
    ILocalStorageService _localStorage;

    /// <summary>
    /// Client browser 의 초기 설정.  Http, NavigationManager 등 blazor component 요소들이 활용가능할 때 수행해야 할 작업 지정
    /// <br/> - Server 로부터 settings, RuntimeModel 가져오기
    /// <br/> - Hub connection 설정
    /// <br/> - CPU running status 변경에 대한 subscription
    /// </summary>
    public async Task InitializeAsync(HttpClient http, NavigationManager navigationManager, ILocalStorageService localStorage)
    {
        _http = http;
        _navigationManager = navigationManager;
        _localStorage = localStorage;

        if (ServerSettings == null)
        {
            ServerSettings = await http.GetFromJsonAsync<ServerSettings>("api/serversettings");
            ModelDsZipPath = ServerSettings.RuntimeModelDsZipPath;
        }

        if (ServerSettings == null)
            Console.Error.WriteLine("Error: ServerSettings is null.");

        base.ClientSettings = await DsClientSettings.ReadAsync(localStorage);

        var result = await http.GetRestResultAsync<RuntimeModelDto>($"api/model");
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
                var result = await http.GetRestResultViaSerialAsync<HMIPackage>($"api/hmi/package");
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
