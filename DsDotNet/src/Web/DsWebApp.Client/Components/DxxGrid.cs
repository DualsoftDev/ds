using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components;

namespace DsWebApp.Client.Components;

public class DxxGrid : DxxGridBase
{
    [Inject] ILocalStorageService LocalStorage { get; set; }
    protected override async Task OnInitializedAsync()
    {
        // base.OnInitializedAsync 이전에 수행.
        base.ClientSettings = await DsClientSettings.ReadAsync(LocalStorage);

        await base.OnInitializedAsync();
    }
    //protected override async Task<ClientSettings> GetClientSettings()
    //{
    //    return (ClientSettings)(await DsClientSettings.ReadAsync(LocalStorage));
    //}
}
