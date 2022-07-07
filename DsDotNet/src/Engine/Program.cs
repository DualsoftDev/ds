using DsParser;

using System;
using System.Linq;
using Engine.Graph;

namespace Engine
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = @"
//[sys] it = {
//    [task] T = {
//        Cp = {P.F.Vp ~ P.F.Sp}
//        Cm = {P.F.Vm ~ P.F.Sm}
//    }
//    [flow] F = {
//        Main = { T.Cp > T.Cm; }
//        //parenting = {A > B > C; C |> B; }
//        //T.C1 <||> T.C2;
//        Main > A, B > C > D, E;
//        T.Cm > T.Cp;
//    }
//}
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
    P.F;
}
";
            var pModel = ModelParser.ParseFromString(text);

            var model = ModelConvertor.Convert(pModel);


            var flows = model.Cpus.SelectMany(cpu => cpu.Flows);
            var graph = GraphGenerator.generateGraph(flows);
            Console.WriteLine("Hello World!");
        }
    }
}
