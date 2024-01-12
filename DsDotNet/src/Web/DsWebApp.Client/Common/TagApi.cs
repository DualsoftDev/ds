using Dual.Web.Blazor.Client.Auth;
using Dual.Web.Blazor.ClientSide;

using Microsoft.AspNetCore.Components;

using ResultSS = Dual.Web.Blazor.Shared.RestResult<string>;

namespace DsWebApp.Client.Common
{
    public static class TagApiExtensions
    {
        public static async Task<ResultSS> PostTagAsync(this HttpClient http, TagWeb tag, AuthenticationStateProvider auth, NavigationManager navigationManager)
        {
            if (await auth.SetAuthHeaderAsync(http))
            {
                return await http.PostAsJsonGetRestResultStringAsync("api/hmi/tag", tag);
            }
            else
            {
                navigationManager.NavigateTo("/toplevel/login");
                return ResultSS.Err("Not Authenticated");
            }
        }
    }
}
