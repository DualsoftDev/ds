using Dual.Web.Blazor.Auth;
using Dual.Web.Server.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dual.Web.Server;

//[Route("api/[controller]")]
//[ApiController]
public class AuthControllerBase(IUserAccountService userAccountService) : ControllerBase
{
    IUserAccountService xUserAccountService = userAccountService;
    //// api/auth/check
    //[Authorize(Roles = "Administrator,User")]
    //[HttpGet("check")]
    //public bool CheckToken() => true;

    //// api/auth/login
    //[HttpPost]
    //[Route("login")]
    //[AllowAnonymous]
    //public ActionResult<UserSession> Login([FromBody] LoginRequest loginRequest)
    //{
    //    var jwtAuthenticationManager = new JwtAuthenticationManager(userAccountService);
    //    var userSession = jwtAuthenticationManager.GenerateJwtToken(loginRequest.UserName, loginRequest.Password);
    //    if (userSession is null)
    //        return Unauthorized();
    //    else
    //        return userSession;
    //}
}
