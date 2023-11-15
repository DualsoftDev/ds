namespace DsWebApp.Server.Common
{
    public class ServerSettings
    {
        public bool UseHttpsRedirection { get; set; }
        public ClientEnvironment ClientEnvironment { get; set; }
    }

    public static class ServerSettingsExtensions
    {
        public static void Initialize(this ServerSettings serverSettings)
        {
            //serverSettings.VncSettings.Initialize();
        }
    }
}
