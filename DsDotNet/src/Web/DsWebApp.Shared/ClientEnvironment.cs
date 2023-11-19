namespace DsWebApp.Shared;

public class ClientEnvironment
{
    public bool IsEnableDebug { get; set; }
    public bool IsShowDemo { get; set; }
    public bool IsShowManagement { get; set; }
    /// <summary>
    /// Real time streaming protocol url
    /// </summary>
    public string RTSPUrl { get; set; }
    //public List<MediaInfo> MediaInfos { get; set; }
}
