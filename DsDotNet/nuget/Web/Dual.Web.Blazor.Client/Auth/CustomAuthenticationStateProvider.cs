// https://www.youtube.com/watch?v=7P_eyz4mEmA

using System.Net.Http.Headers;
using System.Security.Claims;

using Blazored.LocalStorage;
using Blazored.SessionStorage;

using Dual.Common.Core;
using Dual.Web.Blazor.Auth;

using Microsoft.AspNetCore.Components.Authorization;

namespace Dual.Web.Blazor.Client.Auth;


/// <summary>
/// 인증 관련 사용자 정보 제공
/// </summary>
public class CustomAuthenticationStateProvider(ISessionStorageService sessionStorage, ILocalStorageService localStorage) : AuthenticationStateProvider
{
    // https://github.com/codingdroplets/BlazorWasmAuthenticationAndAuthorization/blob/master/Client/Authentication/CustomAuthenticationStateProvider.cs
    private ClaimsPrincipal _anonymous = new (new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            await Console.Out.WriteLineAsync("CustomAuthenticationStateProvider::GetAuthenticationStateAsync(): ");
            var userSession = await sessionStorage.ReadEncryptedItemAsync<UserSession>("UserSession");
            userSession ??= await localStorage.ReadEncryptedItemAsync<UserSession>("UserSession");
            if (userSession == null)
            {
                await Console.Out.WriteLineAsync("Returns Anonymous User.");
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }

            var claims = new List<Claim>() { new(ClaimTypes.Name, userSession.UserName) };
            userSession.Roles?
                .Split(',')
                .Select(r => r.Trim())
                .Iter(role => claims.Add(new Claim(ClaimTypes.Role, role)))
                ;

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "JwtAuth"));

            await Console.Out.WriteLineAsync($"ROLES: Returns {userSession.UserName} with {userSession.Roles}.");
            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }
        catch(Exception ex)
        {
            await Console.Out.WriteLineAsync($"Exception: {ex.Message}");
            return await Task.FromResult(new AuthenticationState(_anonymous));
        }
    }

    public async Task UpdateAuthenticationState(UserSession userSession)
    {
        await Console.Out.WriteLineAsync("CustomAuthenticationStateProvider::UpdateAuthenticationState");
        ClaimsPrincipal claimsPrincipal;

        if (userSession == null)
        {
            claimsPrincipal = _anonymous;
            await sessionStorage.RemoveItemAsync("UserSession");
            await localStorage.RemoveItemAsync("UserSession");
        }
        else
        {
            var claims = new List<Claim>() { new(ClaimTypes.Name, userSession.UserName) };
            userSession.Roles?
                .Split(',')
                .Select(r => r.Trim())
                .Iter(role => claims.Add(new Claim(ClaimTypes.Role, role)))
                ;


            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            userSession.ExpiryTimeStamp = DateTime.Now.AddSeconds(userSession.ExpiresIn);
            await sessionStorage.SaveItemEncryptedAsync("UserSession", userSession);
            await localStorage.SaveItemEncryptedAsync("UserSession", userSession);
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    public async Task<string> GetToken()
    {
        await Console.Out.WriteLineAsync("CustomAuthenticationStateProvider::GetToken()");
        var result = string.Empty;

        try
        {
            var userSession = await sessionStorage.ReadEncryptedItemAsync<UserSession>("UserSession");
            userSession ??= await localStorage.ReadEncryptedItemAsync<UserSession>("UserSession");
            if (userSession != null && DateTime.Now < userSession.ExpiryTimeStamp)
                result = userSession.Token;
        }
        catch {
            await Console.Out.WriteLineAsync("Failed CustomAuthenticationStateProvider::GetToken()");
        }

        return result;
    }
}



public static class CustomAuthExtension
{
    /// <summary>
    /// 브라우저에 저장된 인증정보를 Http Request Header에 추가한다.
    /// </summary>
    public static async Task<bool> SetAuthHeaderAsync(this AuthenticationStateProvider authenticationStateProvider, HttpClient Http)
    {
        var customAuthStateProvider = (CustomAuthenticationStateProvider)authenticationStateProvider;
        var token = await customAuthStateProvider.GetToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            return true;
        }
        return false;
    }
}