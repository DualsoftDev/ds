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
[sys] L = {
    [alias] = {
        P.F.Vp = { Vp1; Vp2; Vp3; }
        P.F.Vm = { Vm1; Vm2; Vm3; }
        L.T.Cp = {Cp1; Cp2; Cp3;}
        L.T.Cm = {Cm1; Cm2; Cm3;}
    }
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
        //Cm1 = {P.F.Vm ~ P.F.Sm, P.F.Sm}
        //Cm2 = {P.F.Vm ~ P.F.Sm}
        //Cm3 = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        //Main = { T.Cp |> T.Cm, T.Cm1 > T.Cm2; T.Cm3 > T.Cm2; }
        //Main = { T.Cp |> T.Cm; }
        Main = { Cp1 |> Cm1; }
        //Main > Weak;
        //Main >> Strong;
        //Main |> XXX;
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
    L.F;
}
";
            var engine = new Engine(text, "Cpu");
            Program.Engine = engine;
            var opc = engine.Opc;

            var resetTag = "Reset_L_F_Main";
            if (engine.Cpu.Tags.ContainsKey(resetTag))
            {
                var children = engine.Cpu.RootFlows.SelectMany(f => f.ChildVertices);
                var main = children.OfType<Segment>().FirstOrDefault(c => c.Name == "Main");
                var edges = main.ChildFlow.Edges.ToArray();

                opc.Write(resetTag, true);
                opc.Write(resetTag, false);
                opc.Write("Start_L_F_Main", true);
                opc.Write(resetTag, true);

                opc.Write("AutoStart_L_F_Main", true);
            }

            engine.Run();
            //var model = ModelParser.ParseFromString(text);
            //foreach (var cpu in model.Cpus)
            //    cpu.Run();

            var flows = engine.Model.Cpus.SelectMany(cpu => cpu.RootFlows.OfType<RootFlow>());
            var graphInfo = GraphUtil.analyzeFlows(flows, true);

            Console.WriteLine("Hello World!");
        }
    }
}
