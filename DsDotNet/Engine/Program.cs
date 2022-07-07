using DsParser;

using System;

namespace Engine
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = @"
[sys] it = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { T.Cp > T.Cm; }
        //parenting = {A > B > C; C |> B; }
        //T.C1 <||> T.C2;
        Main > A, B > C > D, E;
        T.Cm > T.Cp;
    }
}
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;
    }
}
";
            var pModel = ModelParser.ParseFromString(text);

            var model = ModelConvertor.Convert(pModel);
            Console.WriteLine("Hello World!");

        }
    }
}
