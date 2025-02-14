namespace Dual.Web.Blazor.Auth;

/// <summary>
/// User Session 정보 : UserName, Token, Role, Expires
/// </summary>
public class UserSession
{
    public string UserName { get; set; }
    public string Token { get; set; }
    /// <summary>
    /// Comma separated roles
    /// </summary>
    public string Roles { get; set; }
    public int ExpiresIn { get; set; }
    public DateTime ExpiryTimeStamp { get; set; }
}
