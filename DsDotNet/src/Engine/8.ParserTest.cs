using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    internal static class ParserTest
    {
        public static void TestParseSafety()
        {
            var text = @"
[sys] L = {
    [flow] F = {
        Main = { T.Cp > T.Cm; }
        [safety] = {
            Main = {P.F.Sp; P.F.Sm}
            Main2 = {P.F.Sp; P.F.Sm}
        }
    }
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
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

[prop] = {
    [ safety ] = {
        L.F.Main = {P.F.Sp; P.F.Sm}
        L.F.Main2 = {P.F.Sp; P.F.Sm}
    }
}
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] PCpu = {
        P.F;
    }
}

";
            var engine = new EngineBuilder(text, "Cpu").Engine;
            Program.Engine = engine;
            engine.Run();
        }
    }
}
