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
    StandardPC,
    StandardPLC,
    LightPC,
    LightPLC,
    Simulation,
    SimulationDubug,
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
                RuntimePackageCs.StandardPC => RuntimePackage.StandardPC,
                RuntimePackageCs.StandardPLC => RuntimePackage.StandardPLC,
                RuntimePackageCs.Simulation => RuntimePackage.Simulation,
                RuntimePackageCs.SimulationDubug => RuntimePackage.SimulationDubug,
                _ => RuntimePackage.StandardPC,
        };
    public static RuntimePackageCs ToRuntimePackageCs(this RuntimePackage runtimePackage)
    {
        if (runtimePackage == RuntimePackage.StandardPC)
            return RuntimePackageCs.StandardPC;
        else if (runtimePackage == RuntimePackage.StandardPLC)
            return RuntimePackageCs.StandardPLC;
        else if (runtimePackage == RuntimePackage.Simulation)
            return RuntimePackageCs.Simulation;
        else if (runtimePackage == RuntimePackage.SimulationDubug)
            return RuntimePackageCs.Simulation;
        else
            throw new NotImplementedException();        
    }
}
