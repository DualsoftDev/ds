using Dual.Web.Blazor.Auth;

namespace Dual.Web.Blazor.ClientSide;

public class UserAuthInfo : LoginRequest
{
    public bool IsAdmin { get; set; }
}
