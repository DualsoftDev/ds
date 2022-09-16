using Antlr4.Runtime;

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

        bool simulationMode = activeCpuName.IsNullOrEmpty();

        var helper = ModelParser.ParseFromString2(modelText, simulationMode);
        Model = helper.Model;
        Global.Model = Model;

        Data = new DataBroker();

        if (simulationMode)
        {
            Cpu = Model.Cpus.First();
            Cpu.IsActive = true;
        }
        else
        {
            Cpu = Model.Cpus.FirstOrDefault(cpu => cpu.Name == activeCpuName);
            if (Cpu == null)
                throw new Exception($"Failed to find cpu name : [{activeCpuName}]");

            Cpu.IsActive = true;
        }

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
        Model = ModelParser.ParseFromString(modelText, true);
    }
}
