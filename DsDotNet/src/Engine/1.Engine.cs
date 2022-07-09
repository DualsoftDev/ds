using Engine.Core;
using Engine.OPC;

using System.Linq;

namespace Engine
{
    class Engine
    {
        public OpcBroker Opc { get; }
        public Cpu Cpu { get; }
        public Model Model { get; }

        public Engine(string modelText, string activeCpuName)
        {
            Model = ModelParser.ParseFromString(modelText);
            Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
            Cpu.Initialize();

            Opc = new OpcBroker();
            var externalTags = Cpu.CollectTags().OfType<Tag>().Where(t => t.IsExternal);
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
