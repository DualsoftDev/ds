using Engine.Core;
using Engine.OPC;

using System.Linq;

namespace Engine
{
    public class Engine : IEngine
    {
        public OpcBroker Opc { get; }
        public Cpu Cpu { get; }
        public Model Model { get; }

        public Engine(string modelText, string activeCpuName)
        {
            Model = ModelParser.ParseFromString(modelText);
            Opc = new OpcBroker();
            Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
            Cpu.Engine = this;

            this.InitializeFlows(Cpu, Opc);
            //Cpu.InitializeOtherFlows(Opc);
            //Cpu.InitializeMyFlows(Opc);

            var externalTags =
                Cpu.CollectTags()
                    .OfType<Tag>()
                    .Where(t => t.IsExternal)
                    .ToArray();
            Opc.AddTags(externalTags);
        }

        public void Run()
        {
            Cpu.Run();
            //foreach (var cpu in Model.Cpus)
            //    cpu.Run();
        }

    }
}
