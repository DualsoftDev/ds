using Dual.Web.Blazor.Client.Auth;
using Dual.Web.Blazor.ClientSide;

using Microsoft.AspNetCore.Components;

using SimpleResult = Dual.Common.Core.ResultSerializable<string, string>;

namespace DsWebApp.Client.Common
{
    public static class TagApiExtensions
    {
        public static async Task<SimpleResult> PostTagAsync(this HttpClient http, TagWeb tag, AuthenticationStateProvider auth, NavigationManager navigationManager)
        {
            if (await auth.SetAuthHeaderAsync(http))
            {
                return await http.PostAsJsonResultSimpleAsync("api/hmi/tag", tag);
            }
            else
            {
                navigationManager.NavigateTo("/toplevel/login");
                return SimpleResult.Err("Not Authenticated");
            }
        }
    }
}
