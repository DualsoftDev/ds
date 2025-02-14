namespace Dual.Web.Server.Auth;

public class UserAccount
{
    public string UserName { get; set; }
    public string Password { get; set; }

    /// <summary>
    /// Comma separated roles
    /// </summary>
    public string Roles { get; set; }
}
