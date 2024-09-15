using Dual.Common.Base.CS;
using Dual.Common.Core;

using Engine.Core;

using static Engine.Core.RuntimeGeneratorModule;

namespace DsWebApp.Shared;


public class ServerSettings
{
    public string ServiceFolder { get; set; }
    public bool UseHttpsRedirection { get; set; }
    public bool UseIOHub { get; set; }
    public bool AutoStartOnSystemPowerUp { get; set; }
    public ClientEnvironment ClientEnvironment { get; set; }
    public DSCommonAppSettings CommonAppSettings { get; set; }
    public string RuntimeModelDsZipPath {
        get => CommonAppSettings?.LoggerDBSettings?.ModelFilePath;
        set => CommonAppSettings.LoggerDBSettings.ModelFilePath = value; }
    public string ServerUrl { get; set; }
    public double JwtTokenValidityMinutes { get; set; }

    public RuntimePackageCs RuntimePackageCs { get; set; }

    public RuntimePackage GetRuntimePackage() => RuntimePackageCs.ToRuntimePackage();
    public void SetRuntimePackage(RuntimePackage value) => RuntimePackageCs = value.ToRuntimePackageCs();
}

/// <summary>
/// F# RuntimePackage type 을 C# enum 으로 호환하기 위한 용도
/// </summary>
public enum RuntimePackageCs
{
    PC,
    PCSIM,
    PLC,
    PLCSIM,
}


public static class ServerSettingsExtensions
{
    public static void Initialize(this ServerSettings serverSettings, DSCommonAppSettings commonAppSettings)
    {
        serverSettings.CommonAppSettings = commonAppSettings;
        LoggerDBSettings loggerDBSettings = commonAppSettings.LoggerDBSettings;
        serverSettings.RuntimeModelDsZipPath = loggerDBSettings.ModelFilePath;
        serverSettings.SetRuntimePackage(RuntimeGeneratorModule.ToRuntimePackage(loggerDBSettings.DbWriter));
        if (serverSettings.ServiceFolder.NonNullAny())
            Directory.CreateDirectory(serverSettings.ServiceFolder);
        if (serverSettings.RuntimeModelDsZipPath.NonNullAny())
            Directory.CreateDirectory(Path.GetDirectoryName(serverSettings.RuntimeModelDsZipPath));

        if (loggerDBSettings.ConnectionPath.IsNullOrEmpty())
            throw new Exception("LoggerDBSettings.ConnectionPath is not set.");
    }

    public static RuntimePackage ToRuntimePackage(this RuntimePackageCs runtimePackageCs) =>
        runtimePackageCs switch
        {
                RuntimePackageCs.PC => RuntimePackage.PC,
                RuntimePackageCs.PLC => RuntimePackage.PLC,
                RuntimePackageCs.PCSIM => RuntimePackage.PCSIM,
                RuntimePackageCs.PLCSIM => RuntimePackage.PLCSIM,
                _ => RuntimePackage.PC,
        };
    public static RuntimePackageCs ToRuntimePackageCs(this RuntimePackage runtimePackage)
    {
        if (runtimePackage == RuntimePackage.PC)
            return RuntimePackageCs.PC;
        else if (runtimePackage == RuntimePackage.PLC)
            return RuntimePackageCs.PLC;
        else if (runtimePackage == RuntimePackage.PCSIM)
            return RuntimePackageCs.PCSIM;
        else if (runtimePackage == RuntimePackage.PLCSIM)
            return RuntimePackageCs.PLCSIM;
        else
            throw new NotImplementedException();
    }
}
