using Engine.Parser;
using Engine.Runner;
using System.Threading.Tasks;
using static Engine.Runner.DataModule;

namespace Engine;

public class EngineBuilder
{
    public DataBroker Data { get; }
    public Cpu Cpu { get; }
    public Model Model { get; }
    public ENGINE Engine { get; }

    public EngineBuilder(string modelText, string activeCpuName)
    {
        EngineModule.Initialize();

        Model = ModelParser.ParseFromString(modelText);
        Global.Model = Model;

        Data = new DataBroker();
        Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
        Cpu.IsActive = true;

        Model.BuildGraphInfo();

        Model.Epilogue(Data);

        //if (Global.IsControlMode)
        //    Task.Run(async () => { await Data.CommunicationPLC(); })
        //        .FireAndForget();
        Data.CommunicationPLC();
        foreach (var cpu in Model.Cpus)
            cpu.PrintTags();

        Engine = new ENGINE(Model, Data, Cpu);
        Cpu.Engine = Engine;
        Task.Run(() => { Data.StreamData(); })
            .FireAndForget();
    }

    /// <summary> Used for Unit test only.</summary>
    internal EngineBuilder(string modelText)
    {
        EngineModule.Initialize();
        Data = new DataBroker();
        Model = ModelParser.ParseFromString(modelText);
    }
}
