//using IoHubClient = IO.Core.Client;

using DsWebApp.Shared;

using Dual.Web.Blazor.ServerSide;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using System.Net.Http.Json;

using static System.Net.WebRequestMethods;

namespace DsWebApp.Client
{
    public class ClientGlobal
    {
        public HmiTagPackage HmiTagPackage { get; set; }

        ServerSettings _serverSettings;
        public async Task<ServerSettings> GetServerSettingsAsync(HttpClient http)
        {
            if (_serverSettings == null)
                _serverSettings = await http.GetFromJsonAsync<ServerSettings>("api/serversettings");

            return _serverSettings;
        }

        RuntimeModelDto _modelDto;
        public async Task<RuntimeModelDto> GetModelDtoAsync(HttpClient http)
        {
            if (_modelDto == null)
            {
                var response = await http.GetAsync($"api/model");
                if (response.IsSuccessStatusCode)
                {
                    _modelDto = await response.Content.ReadFromJsonAsync<RuntimeModelDto>();
                    Console.WriteLine($"Got path={_modelDto.SourceDsZipPath}, isCpuRunning={_modelDto.IsCpuRunning}");
                }
                else
                {
                    // 실패한 응답 코드에 대한 처리
                    // 예: 사용자에게 에러 메시지 표시, 로깅 등
                }
            }

            return _modelDto;
        }
        public async Task<IDisposable> MonitorModelChangeAsync(NavigationManager navigationManager, Action<RuntimeModelDto> onModelChanged)
        {
            HubConnection hubConnection = await navigationManager.ToAbsoluteUri("/hub/model").StartHubAsync();
            IDisposable subscription =
                hubConnection.On<RuntimeModelDto>(SK.S2CNModelChanged, (RuntimeModelDto newModel) =>
                {
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
}
