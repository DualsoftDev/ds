namespace Dual.Web.Blazor.Auth;

/// <summary>
/// Login Request 정보 : UserName, Password
/// </summary>
public class LoginRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
}
