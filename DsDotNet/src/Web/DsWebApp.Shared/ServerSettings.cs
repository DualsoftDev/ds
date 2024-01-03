using System.Text.Json.Serialization;

using static Engine.Core.RuntimeGeneratorModule;

namespace DsWebApp.Shared;


public class ServerSettings
{
    public bool UseHttpsRedirection { get; set; }
    public bool AutoStartOnSystemPowerUp { get; set; }
    public ClientEnvironment ClientEnvironment { get; set; }
    public string RuntimeModelDsZipPath { get; set; }
    public double JwtTokenValidityMinutes { get; set; }

    public RuntimePackageCs RuntimePackageCs { get; set; }

    public RuntimePackage GetRuntimePackage() =>
        RuntimePackageCs switch
        {
            RuntimePackageCs.SimulationDubug => RuntimePackage.SimulationDubug,
            RuntimePackageCs.Simulation => RuntimePackage.Simulation,
            RuntimePackageCs.StandardPC => RuntimePackage.StandardPC,
            RuntimePackageCs.StandardPLC => RuntimePackage.StandardPLC,
            RuntimePackageCs.LightPC => RuntimePackage.LightPC,
            RuntimePackageCs.LightPLC => RuntimePackage.LightPLC,
            _ => RuntimePackage.StandardPC,
        };
    public void SetRuntimePackage(RuntimePackage value)
    {
        if (value == RuntimePackage.SimulationDubug)
            RuntimePackageCs = RuntimePackageCs.SimulationDubug;
        else if (value == RuntimePackage.Simulation)
            RuntimePackageCs = RuntimePackageCs.Simulation;
        else if (value == RuntimePackage.StandardPC)
            RuntimePackageCs = RuntimePackageCs.StandardPC;
        else if (value == RuntimePackage.StandardPLC)
            RuntimePackageCs = RuntimePackageCs.StandardPLC;
        else if (value == RuntimePackage.LightPC)
            RuntimePackageCs = RuntimePackageCs.LightPC;
        else if (value == RuntimePackage.LightPLC)
            RuntimePackageCs = RuntimePackageCs.LightPLC;
        else
            throw new NotImplementedException();
    }
}

/// <summary>
/// F# RuntimePackage type 을 C# enum 으로 호환하기 위한 용도
/// </summary>
public enum RuntimePackageCs
{
    SimulationDubug,
    Simulation,
    StandardPC,
    StandardPLC,
    LightPC,
    LightPLC,
}


public static class ServerSettingsExtensions
{
    public static void Initialize(this ServerSettings serverSettings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(serverSettings.RuntimeModelDsZipPath));
        //serverSettings.VncSettings.Initialize();
    }
}
