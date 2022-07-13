using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;
using Engine.Graph;
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
            Model.Epilogue();

            Opc = new OpcBroker();
            Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
            Cpu.Engine = this;

            this.InitializeFlows(Cpu, Opc);

            Debug.Assert(Opc._opcTags.All(t => t.OriginalTag.IsExternal));
            Opc.Print();

            Model.Cpus.Iter(cpu => readTagsFromOpc(cpu));

            void readTagsFromOpc(Cpu cpu)
            {
                var tpls = Opc.ReadTags(cpu.Tags.Select(t => t.Key)).ToArray();
                foreach ((var tName, var value) in tpls)
                {
                    var tag = cpu.Tags[tName];
                    if (tag.Value != value)
                        cpu.OnOpcTagChanged(tName, value);
                }
            }
        }

        public void Run()
        {
            Cpu.Run();
            FakeCpu.Run();
        }

    }

    public static class EngineExtension
    {

    }

    public static class ModelExtension
    {
        public static void Epilogue(this Model model)
        {
            var rootFlows = model.Systems.SelectMany(sys => sys.RootFlows);
            var subFlows = rootFlows.SelectMany(rf => rf.SubFlows);
            var allFlows = rootFlows.Cast<Flow>().Concat(subFlows);
            foreach (var flow in allFlows)
                flow.GraphInfo = GraphUtil.analyzeFlows(new[] { flow });

            foreach(var cpu in model.Cpus)
                cpu.GraphInfo = GraphUtil.analyzeFlows(cpu.RootFlows);
        }
    }

}
