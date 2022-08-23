using Engine.Parser;
using Engine.Runner;

namespace Engine;

public class EngineBuilder
{
    public OpcBroker Opc { get; }
    public Cpu Cpu { get; }
    public Model Model { get; }
    public ENGINE Engine { get; }

    public EngineBuilder(string modelText, string activeCpuName)
    {
        EngineModule.Initialize();

        Model = ModelParser.ParseFromString(modelText);

        Opc = new OpcBroker();
        Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
        Cpu.IsActive = true;

        Model.BuildGraphInfo();

        Model.Epilogue(Opc);

        Opc.Print();
        foreach(var cpu in Model.Cpus)
            cpu.PrintTags();

        Engine = new ENGINE(Model, Opc, Cpu);
        Cpu.Engine = Engine;
    }

    /// <summary> Used for Unit test only.</summary>
    internal EngineBuilder(string modelText)
    {
        EngineModule.Initialize();
        Opc = new OpcBroker();
        Model = ModelParser.ParseFromString(modelText);
    }
}
