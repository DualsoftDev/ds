using Dual.Web.Blazor.Auth;
using Dual.Web.Blazor.ClientSide;
using Dual.Web.Server.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;


namespace TwmApp.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IUserAccountService userAccountService, ServerGlobal global) : AuthControllerBase(userAccountService)
{
    // api/auth/check
    [HttpGet("check")]
    [Authorize(Roles = "Administrator,User")]
    public bool CheckToken() => true;


    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    public ActionResult<UserSession> Login([FromBody] LoginRequest loginRequest)
    {
        var jwtAuthenticationManager = new JwtAuthenticationManager(userAccountService);
        var userSession = jwtAuthenticationManager.GenerateJwtToken(loginRequest.UserName, loginRequest.Password);
        if (userSession is null)
            return Unauthorized();
        else
            return userSession;
    }


    // api/auth/adduser
    [HttpPost]
    [Route("adduser")]    
    [Authorize(Roles = "Administrator")]
    
    public ErrorMessage AddUser([FromBody] UserAuthInfo loginRequest)
    {
        try
        {
            var (u, p, a) = (loginRequest.UserName, loginRequest.Password, loginRequest.IsAdmin);
            var encrypted = p.IsNullOrEmpty() ? null : Dual.Common.Utils.Crypto.Encrypt(p, K.CryptKey);

            using var conn = global.CreateDbConnection();

            var userTable = "user";
            var existing = conn.QueryFirstOrDefault<UserAuthInfo>($"SELECT [password], [isAdmin] FROM [{userTable}] WHERE [username] = @UserName;", new { UserName = u });
            var newInfo = new { UserName = u, Password = encrypted, IsAdmin = a };
            if (existing == null)
            {
                conn.Execute(
                    $"INSERT INTO [{userTable}] (userName, password, isAdmin) VALUES (@UserName, @Password, @IsAdmin);"
                    , newInfo);
            }
            else
            {
                conn.Execute(
                    $@"UPDATE [{userTable}]
                        SET [password] = @Password, [isAdmin] = @IsAdmin
                        WHERE [username] = @UserName;"
                    , newInfo);
            }
            return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
