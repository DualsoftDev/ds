using Dual.Web.Blazor.Client.Auth;
using Dual.Web.Blazor.ClientSide;
using Dual.Web.Blazor.Shared;

using Microsoft.AspNetCore.Components;

using RestResultString = Dual.Web.Blazor.Shared.RestResult<string>;

namespace DsWebApp.Client.Common
{
    public static class NugetCommonCandidatesExtensions
    {
        /// <summary>
        /// Server side 에서는 RestResult(string) 으로 반환하는 API 에 대해서 RestResult(T) type 으로 변환해서 반환
        /// <br/> - Server side API 에서는 Newtonsoft.Json 을 이용해서 serialize 해 주어야 한다.
        /// </summary>
        public static async Task<RestResult<T>> GetRestResultViaSerialAsync<T>(this HttpClient http, string api)
        {
            var restStrResult = await http.GetRestResultStringAsync(api);
            var package = NewtonsoftJson.DeserializeObject<T>(restStrResult.Value);
            return package;
        }
        public static Task<RestResultString> GetRestResultStringAsync(this HttpClient http, string api) => http.GetRestResultAsync<string>(api);

    }
}
