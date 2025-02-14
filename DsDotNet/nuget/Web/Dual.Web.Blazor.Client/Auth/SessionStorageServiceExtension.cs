using Blazored.SessionStorage;
using System.Text;
using System.Text.Json;
using Dual.Common.Core;

namespace Dual.Web.Blazor.Client.Auth;

public static class SessionStorageServiceExtension
{
    public static async Task SaveItemEncryptedAsync<T>(this ISessionStorageService sessionStorageService, string key, T item)
    {
        var itemJson = NewtonsoftJson.SerializeObject(item);
        var itemJsonBytes = Encoding.UTF8.GetBytes(itemJson);
        var base64Json = Convert.ToBase64String(itemJsonBytes);
        await sessionStorageService.SetItemAsync(key, base64Json);
    }

    public static async Task<T> ReadEncryptedItemAsync<T>(this ISessionStorageService sessionStorageService, string key) where T:class
    {
        await Console.Out.WriteLineAsync($"ReadEncryptedItemAsync(sessionStorageService={sessionStorageService}, key={key}): ");
        var base64Json = await sessionStorageService.GetItemAsync<string>(key);
        if (base64Json.IsNullOrEmpty())
            return null;
        var itemJsonBytes = Convert.FromBase64String(base64Json);
        var itemJson = Encoding.UTF8.GetString(itemJsonBytes);
        var item = NewtonsoftJson.DeserializeObject<T>(itemJson);
        return item;
    }
}
