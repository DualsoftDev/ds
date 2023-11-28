namespace DsWebApp.Shared;

public class ServerSettings
{
    public bool UseHttpsRedirection { get; set; }
    public bool AutoStartOnSystemPowerUp { get; set; }
    public ClientEnvironment ClientEnvironment { get; set; }
    public string RuntimeModelDsZipPath { get; set; }
    public double JwtTokenValidityMinutes { get; set; }
}

public static class ServerSettingsExtensions
{
    public static void Initialize(this ServerSettings serverSettings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(serverSettings.RuntimeModelDsZipPath));
        //serverSettings.VncSettings.Initialize();
    }
}
