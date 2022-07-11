using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.OPC;

using System.Diagnostics;
using System.Linq;

namespace Engine
{
    public class Engine : IEngine
    {
        public OpcBroker Opc { get; }
        public Cpu Cpu { get; }
        public FakeCpu FakeCpu { get; set; }
        public Model Model { get; }

        public Engine(string modelText, string activeCpuName)
        {
            Model = ModelParser.ParseFromString(modelText);
            Opc = new OpcBroker();
            Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
            Cpu.Engine = this;

            this.InitializeFlows(Cpu, Opc);

            Debug.Assert(Opc._opcTags.All(t => t.OriginalTag.IsExternal));
            Opc.Print();

            Opc.Write("Start_it_F_Main", true);
        }

        public void Run()
        {
            Cpu.Run();
            FakeCpu.Run();
        }
    }
}
