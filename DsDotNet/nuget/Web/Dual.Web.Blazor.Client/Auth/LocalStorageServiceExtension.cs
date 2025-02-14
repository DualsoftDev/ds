using Blazored.LocalStorage;
using Blazored.SessionStorage;

using Dual.Common.Core;

using System.Text;
using System.Text.Json;

namespace Dual.Web.Blazor.Client.Auth;

public static class LocalStorageServiceExtension
{
    public static async Task SaveItemEncryptedAsync<T>(this ILocalStorageService storageService, string key, T item)
    {
        var itemJson = NewtonsoftJson.SerializeObject(item);
        var itemJsonBytes = Encoding.UTF8.GetBytes(itemJson);
        var base64Json = Convert.ToBase64String(itemJsonBytes);
        await storageService.SetItemAsync(key, base64Json);
    }

    public static async Task<T> ReadEncryptedItemAsync<T>(this ILocalStorageService storageService, string key) where T:class
    {
        try
        {
            await Console.Out.WriteLineAsync($"ReadEncryptedItemAsync(sessionStorageService={storageService}, key={key}): ");
            var base64Json = await storageService.GetItemAsync<string>(key);
            if (base64Json.IsNullOrEmpty())
                return null;

            var itemJsonBytes = Convert.FromBase64String(base64Json);
            var itemJson = Encoding.UTF8.GetString(itemJsonBytes);
            var item = NewtonsoftJson.DeserializeObject<T>(itemJson);
            return item;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
