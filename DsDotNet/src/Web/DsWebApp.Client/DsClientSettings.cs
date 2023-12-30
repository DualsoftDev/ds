using Blazored.LocalStorage;

namespace DsWebApp.Client;

public class DsClientSettings : ClientSettings
{
    public new async Task SaveAsync(ILocalStorageService localStorage) =>
        await localStorage.SetItemAsync("ClientSettings", this);

    public new static async Task<DsClientSettings> ReadAsync(ILocalStorageService localStorage) =>
        await localStorage.GetItemAsync<DsClientSettings>("ClientSettings") ?? new DsClientSettings();
}
