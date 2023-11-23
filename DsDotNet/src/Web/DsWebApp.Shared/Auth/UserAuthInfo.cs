using Dual.Web.Blazor.Auth;

namespace DsWebApp.Shared.Auth;

public class UserAuthInfo : LoginRequest
{
    public bool IsAdmin { get; set; }
}
