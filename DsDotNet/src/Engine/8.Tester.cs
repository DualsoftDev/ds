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
        Cm1 = {P.F.Vm ~ P.F.Sm, P.F.Sm}
        Cm2 = {P.F.Vm ~ P.F.Sm}
        Cm3 = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { T.Cp > T.Cm, T.Cm1 > T.Cm2; T.Cm3 > T.Cm2; }
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

            var resetTag = "Reset_it_F_Main";
            if (engine.Cpu.Tags.ContainsKey(resetTag))
            {
                var children = engine.Cpu.RootFlows.SelectMany(f => f.Children);
                var main = children.OfType<Segment>().FirstOrDefault(c => c.Name == "Main");
                var edges = main.ChildFlow.Edges.ToArray();

                opc.Write(resetTag, true);
                opc.Write(resetTag, false);
                opc.Write("Start_it_F_Main", true);
                opc.Write(resetTag, true);

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
