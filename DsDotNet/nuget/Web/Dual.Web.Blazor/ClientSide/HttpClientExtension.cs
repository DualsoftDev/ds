using Dual.Common.Core;
using Dual.Web.Blazor.Shared;

using Newtonsoft.Json;
using System.Net.Http.Json;

//using SimpleResult = Dual.Web.Blazor.Shared.RestResult<string>;

namespace Dual.Web.Blazor.ClientSide;

public static class HttpClientExtension
{
    /// <summary>
    /// Outer Error: string.  e.g no connection, unauthorized, ...
    /// <br/>
    /// Inner : ResultSerializable[T, Err]
    ///     Err: 서버로부터 받은 error message
    /// <br/>
    /// </summary>
    /// <typeparam name="T">성공시의 return type</typeparam>
    /// <typeparam name="Err">서버 반환 error message</typeparam>
    /// <param name="api">접속할 rest api URL</param>
    /// <returns></returns>
    public static async Task<ResultSerializable<ResultSerializable<T, Err>, ErrorMessage>> GetResultAsync<T, Err>(this HttpClient http, string api)
    {
        var response = await http.GetAsync(api);
        if (response == null)
        {
            Console.WriteLine($"No response");
            return ResultSerializable<ResultSerializable<T, Err>, ErrorMessage>.Err("Response is null");
        }
        else if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            // System.Text.Json.JsonSerializer.Deserialize Not working.  use Newtonsoft.Json
            ResultSerializable<T, Err> jobj = NewtonsoftJson.DeserializeObject<ResultSerializable<T, Err>>(json);
            Console.WriteLine($"Result jobj: IsOK={jobj.IsOk}, Value={jobj.Value}, Err={jobj.Error}");
            return jobj;
        }
        else
        {
            Console.WriteLine($"Error with {response.StatusCode}");
            return ResultSerializable<ResultSerializable<T, Err>, ErrorMessage>.Err($"Error Status code:{response.StatusCode}");
        }
    }


    /// <summary>
    /// Simple get result of type T.  error message 는 모두 string type 
    /// </summary>
    public static async Task<RestResult<T>> GetRestResultAsync<T>(this HttpClient http, string api)
    {
        var response = await http.GetAsync(api);
        if (response == null)
        {
            var msg = $"No response for API: {api}";
            Console.Error.WriteLine(msg);
            return RestResult<T>.Err(msg);
        }
        else if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            // System.Text.Json.JsonSerializer.Deserialize Not working.  use Newtonsoft.Json
            RestResult<T> jobj = NewtonsoftJson.DeserializeObject<RestResult<T>>(json);
            //Console.WriteLine($"Result jobj: IsOK={jobj.IsOk}, Value={jobj.Value}, Err={jobj.Error}");
            return jobj;
        }
        else
        {
            Console.WriteLine($"Error with {response.StatusCode}");
            return RestResult<T>.Err($"Error Status code:{response.StatusCode}");
        }
    }

    /// <summary>
    /// Server side 에서는 RestResult(string) 으로 반환하는 API 에 대해서 RestResult(T) type 으로 변환해서 반환
    /// <br/> - Server side API 에서는 Newtonsoft.Json 을 이용해서 serialize 해 주어야 한다.
    /// </summary>
    public static async Task<RestResult<T>> GetDeserializedObjectAsycn<T>(this HttpClient http, string api)
    {
        var result = await http.GetRestResultStringAsync(api);
        if (result.IsOk)
            return NewtonsoftJson.DeserializeObject<T>(result.Value);

        return RestResult<T>.Err(result.Error);
    }
    public static Task<RestResult<string>> GetRestResultStringAsync(this HttpClient http, string api) => http.GetRestResultAsync<string>(api);


    /// <summary>
    /// RestResult<R> 을 반환하는 Post method (실패시 실패 사유, 성공시 R) 를 쉽게 사용하도록 wrapping
    /// 
    /// R: Return result type
    /// Q: Query type
    /// 
    /// 내부 / 외부 error 를 모두 string 으로 반환
    /// </summary>
    public static async Task<RestResult<R>> PostAsJsonGetRestResultAsync<Q, R>(this HttpClient http, string api, Q value)
    {
        HttpResponseMessage httpResponseMessage = await http.PostAsJsonAsync(api, value);
        if (httpResponseMessage == null)
        {
            Console.WriteLine("No response");
            return RestResult<R>.Err("Response is null");
        }

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            httpResponseMessage.EnsureSuccessStatusCode();
            string content = await httpResponseMessage.Content.ReadAsStringAsync();
            return NewtonsoftJson.DeserializeObject<RestResult<R>>(content);
        }

        Console.WriteLine($"Error with {httpResponseMessage.StatusCode}");
        return RestResult<R>.Err($"Error Status code:{httpResponseMessage.StatusCode}");
    }

    /// <summary>
    /// SimpleResult 을 반환하는 Post method (실패시 실패 사유, 성공시 null or empty) 를 쉽게 사용하도록 wrapping
    /// 
    /// 내부 / 외부 error 를 모두 string 으로 반환
    /// </summary>
    public static async Task<RestResult<string>> PostAsJsonGetRestResultStringAsync<T>(this HttpClient http, string api, T value) =>
        await http.PostAsJsonGetRestResultAsync<T, string>(api, value);

    public static async Task<RestResult<string>> DeleteResultSimpleAsync(this HttpClient http, string api)
    {
        HttpResponseMessage httpResponseMessage = await http.DeleteAsync(api);
        if (httpResponseMessage == null)
        {
            Console.WriteLine("No response");
            return RestResult<string>.Err("Response is null");
        }

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            httpResponseMessage.EnsureSuccessStatusCode();
            string content = await httpResponseMessage.Content.ReadAsStringAsync();
            return NewtonsoftJson.DeserializeObject<RestResult<string>>(content);
        }

        Console.WriteLine($"Error with {httpResponseMessage.StatusCode}");
        return RestResult<string>.Err($"Error Status code:{httpResponseMessage.StatusCode}");
    }
}
