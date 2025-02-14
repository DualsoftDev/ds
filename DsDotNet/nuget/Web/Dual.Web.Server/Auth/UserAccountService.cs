namespace Dual.Web.Server.Auth;

public interface IUserAccountService
{
    UserAccount GetUserAccountByUserName(string userName);
    double JwtTokenValidityMinutes { get; }
}

/// <summary>
/// "user" and "admin" 두개의 계정만 사용하는 default acount service
/// </summary>
public class SimpleUserAccountService : IUserAccountService
{
    private List<UserAccount> _userAccountList;

    public SimpleUserAccountService()
    {
        _userAccountList = new List<UserAccount>
        {
            new UserAccount{ UserName = "admin", Password = "admin", Roles = "Administrator" },
            new UserAccount{ UserName = "user", Password = "user", Roles = "User" }
        };
    }

    public double JwtTokenValidityMinutes => 180;

    public UserAccount GetUserAccountByUserName(string userName)
    {
        return _userAccountList.FirstOrDefault(x => x.UserName == userName);
    }
}

public class UserAccountService(Func<string, UserAccount> funcGetUserAccountByName) : IUserAccountService
{
    public double JwtTokenValidityMinutes { get; set; } = 180;

    public UserAccount GetUserAccountByUserName(string userName) => funcGetUserAccountByName(userName);
}


