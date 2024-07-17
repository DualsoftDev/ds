using Dual.Web.Blazor.Client.Auth;
using Dual.Web.Blazor.ClientSide;

using Microsoft.AspNetCore.Components;

using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;

namespace DsWebApp.Client.Common
{
    public static class TagApiExtensions
    {
        public static async Task<RestResultString> PostTagAsync(this HttpClient http, TagWeb tag, AuthenticationStateProvider auth, Action<string> onError=null)
        {
            if (await auth.SetAuthHeaderAsync(http))
            {
                var result = await http.PostAsJsonGetRestResultStringAsync("api/hmi/tag", tag);
                if (onError != null && result.IsError)
                    onError(result.Error);

                return result;
            }

            return RestResultString.Err("Not Authenticated");
        }
        //public static async Task<RestResultString> PostTagAsync(this HttpClient http, TagWeb tag, AuthenticationStateProvider auth, NavigationManager navigationManager)
        //{
        //    if (await auth.SetAuthHeaderAsync(http))
        //    {
        //        return await http.PostAsJsonGetRestResultStringAsync("api/hmi/tag", tag);
        //    }
        //    else
        //    {
        //        navigationManager.NavigateTo("/toplevel/login");
        //        return RestResultString.Err("Not Authenticated");
        //    }
        //}
    }
}
