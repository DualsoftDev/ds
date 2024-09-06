using Dual.Web.Blazor.Auth;
using Dual.Web.Blazor.ClientSide;
using Dual.Web.Server.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;

namespace TwmApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : AuthControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ServerGlobal _global;

        // Constructor
        public AuthController(IUserAccountService userAccountService, ServerGlobal global)
            : base(userAccountService)
        {
            _userAccountService = userAccountService;
            _global = global;
        }

        // api/auth/check
        [HttpGet("check")]
        [Authorize(Roles = "Administrator,User")]
        public bool CheckToken() => true;

        // api/auth/login
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public ActionResult<UserSession> Login([FromBody] LoginRequest loginRequest)
        {
            var jwtAuthenticationManager = new JwtAuthenticationManager(_userAccountService);
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

                using var conn = _global.CreateDbConnection();

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
}
