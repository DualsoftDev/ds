using static Engine.Core.RuntimeGeneratorModule;

namespace DsWebApp.Shared;


public class ServerSettings
{
    public bool UseHttpsRedirection { get; set; }
    public bool AutoStartOnSystemPowerUp { get; set; }    
    public ClientEnvironment ClientEnvironment { get; set; }
    public string RuntimeModelDsZipPath { get; set; }
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
    PLC,
    LightPC,
    LightPLC,
    Simulation,
    Developer,
}


public static class ServerSettingsExtensions
{
    public static void Initialize(this ServerSettings serverSettings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(serverSettings.RuntimeModelDsZipPath));
        //serverSettings.VncSettings.Initialize();
    }

    public static RuntimePackage ToRuntimePackage(this RuntimePackageCs runtimePackageCs) =>
        runtimePackageCs switch
        {
                RuntimePackageCs.PC => RuntimePackage.PC,
                RuntimePackageCs.PLC => RuntimePackage.PLC,
                RuntimePackageCs.Simulation => RuntimePackage.Simulation,
                RuntimePackageCs.Developer => RuntimePackage.Developer,
                _ => RuntimePackage.PC,
        };
    public static RuntimePackageCs ToRuntimePackageCs(this RuntimePackage runtimePackage)
    {
        if (runtimePackage == RuntimePackage.PC)
            return RuntimePackageCs.PC;
        else if (runtimePackage == RuntimePackage.PLC)
            return RuntimePackageCs.PLC;
        else if (runtimePackage == RuntimePackage.Simulation)
            return RuntimePackageCs.Simulation;
        else if (runtimePackage == RuntimePackage.Developer)
            return RuntimePackageCs.Simulation;
        else
            throw new NotImplementedException();        
    }
}
