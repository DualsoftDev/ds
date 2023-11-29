using Dual.Web.Blazor.Client.Auth;
using Dual.Web.Blazor.ClientSide;

using Microsoft.AspNetCore.Components;

namespace DsWebApp.Client.Common
{
    public static class TagApiExtensions
    {
        public static async Task<ErrorMessage> PostTagAsync(this HttpClient http, TagWeb tag, AuthenticationStateProvider auth, NavigationManager navigationManager)
        {
            if (await auth.SetAuthHeaderAsync(http))
            {
                var anyError = await http.PostAsJsonResultSimpleAsync("api/hmi/tag", tag);
                return anyError;
            }
            else
            {
                navigationManager.NavigateTo("/toplevel/login");
                return "Not Authenticated";
            }
        }
    }
}
