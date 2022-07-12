using System;
using System.Linq;

using Engine.Core;
using Engine.Graph;

namespace Engine
{
    class Tester
    {
        public static void DoSampleTest()
        {
            var text = @"
[sys] it = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { T.Cp > T.Cm; }
        Main > Weak;
        Main >> Strong;
        Main |> XXX;
        //parenting = {A > B > C; C |> B; }
        //T.C1 <||> T.C2;
        //A, B > C > D, E;
        T.Cm > T.Cp;
        T.Cm |> T.Cp;
    }
}
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Pp |> Sm;
        Pm |> Sp;
        Vp <||> Vm;
    }
}
[cpu] Cpu = {
    it.F;
}
";
            var engine = new Engine(text, "Cpu");
            Program.Engine = engine;
            var opc = engine.Opc;

            var startTag = "Reset_it_F_Main";
            if (engine.Cpu.Tags.ContainsKey(startTag))
            {
                var main = engine.Cpu.RootFlows.SelectMany(f => f.Children).FirstOrDefault(c => c.Name == "Main") as Segment;
                var edges = main.ChildFlow.Edges.ToArray();

                opc.Write(startTag, true);
                opc.Write(startTag, false);
                opc.Write(startTag, true);

                opc.Write("AutoStart_it_F_Main", true);
            }

            engine.Run();
            //var model = ModelParser.ParseFromString(text);
            //foreach (var cpu in model.Cpus)
            //    cpu.Run();

            var flows = engine.Model.Cpus.SelectMany(cpu => cpu.RootFlows.OfType<RootFlow>());
            var graphInfo = GraphUtil.analyzeFlows(flows);

            Console.WriteLine("Hello World!");
        }
    }
}
