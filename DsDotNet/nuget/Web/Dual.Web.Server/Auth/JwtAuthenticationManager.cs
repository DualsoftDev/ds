// https://www.youtube.com/watch?v=7P_eyz4mEmA

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dual.Web.Blazor.Auth;
using Dual.Common.Core;
using System.Diagnostics;
using Dual.Common.Base.CS;

namespace Dual.Web.Server.Auth;

public class JwtAuthenticationManager(IUserAccountService userAccountService)
{
    public const string JWT_SECURITY_KEY = "yPkCqn4kSWLtaJwXvN2jGzpQRyTZ3gdXkt7FeBJP";

    public UserSession GenerateJwtToken(string userName, string password)
    {
        Trace.WriteLine($"GenerateJwtToken: {userName} / {password}");
        if (string.IsNullOrWhiteSpace(userName))
        {
            Trace.WriteLine("Null username for jwtToken");
            return null;
        }

        /* Validating the User Credentials */
        var userAccount = userAccountService.GetUserAccountByUserName(userName);
        if (userAccount == null)
        {
            Trace.WriteLine($"GenerateJwtToken: Failed to get userAccountService.GetUserAccountByUserName for {userName}");
            return null;
        }

        var uap = userAccount.Password ?? "";
        var pwd = password ?? "";

        if ( (    uap == "" && pwd != "")
            || (  uap != "" && pwd == "") )
        {
            Trace.WriteLine($"GenerateJwtToken: {uap} != {pwd}");
            return null;
        }

        if (uap != pwd)
        {
            Trace.WriteLine($"GenerateJwtToken: {uap} <> {pwd}");
            return null;
        }

        /* Generating JWT Token */
        Console.WriteLine($"GenerateJwtToken ({userAccountService.JwtTokenValidityMinutes} min): {userAccount.UserName} / {userAccount.Roles}");
        var tokenExpiryTimeStamp = DateTime.Now.AddMinutes(userAccountService.JwtTokenValidityMinutes);
        var tokenKey = Encoding.ASCII.GetBytes(JWT_SECURITY_KEY);

        var claims = new List<Claim>() { new(ClaimTypes.Name, userAccount.UserName) };
        userAccount.Roles?
            .Split(',')
            .Select(r => r.Trim())
            .Iter(role => claims.Add(new Claim(ClaimTypes.Role, role)))
            ;

        var claimsIdentity = new ClaimsIdentity(claims);
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(tokenKey),
            SecurityAlgorithms.HmacSha256Signature);
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = tokenExpiryTimeStamp,
            SigningCredentials = signingCredentials
        };

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor);
        var token = jwtSecurityTokenHandler.WriteToken(securityToken);

        /* Returning the User Session object */
        var userSession = new UserSession
        {
            UserName = userAccount.UserName,
            Roles = userAccount.Roles,
            Token = token,
            ExpiresIn = (int)tokenExpiryTimeStamp.Subtract(DateTime.Now).TotalSeconds
        };

        var json = NewtonsoftJson.SerializeObject(userSession);
        Console.WriteLine($"GenerateJwtToken: Success.  For details, see VisualStudio Output window");
        Trace.WriteLine($"GenerateJwtToken: Success with\r\n{json}");
        return userSession;
    }
}
