using Engine.Parser.FS;

using static Engine.Core.CoreModule;

namespace Engine.Sample;

internal class SampleRunner
{
    public static void Run(string text)
    {
        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(".", "ActiveCpuName"));
        var system = helper.TheSystem.Value;

        Trace.WriteLine("---- Spit result");
        var spits = system.Spit();
        foreach (var spit in spits)
        {
            var tName = spit.GetCore().GetType().Name;
            var name = spit.NameComponents.Combine();
            Trace.WriteLine($"{name}:{tName}");
        }

        var spitObjs = spits.Select(spit => spit.GetCore());
        var flowGraphs = spitObjs.OfType<Flow>().Select(f => f.Graph);
        var segGraphs = spitObjs.OfType<Real>().Select(s => s.Graph);
        foreach (var gr in flowGraphs)
            gr.Dump();
        foreach (var gr in segGraphs)
            gr.Dump();

        System.Console.WriteLine("Done");
    }

    public static string CreateCylinder(string name) => $"[sys] {name} =\r\n" + @"{
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <||> Vm;
    }
    [interfaces] = {
        ""+"" = { F.Vp ~ F.Sp }
        ""-"" = { F.Vm ~ F.Sm }
        // 정보로서의 상호 리셋
        ""+"" <||> ""-"";
    }
}";

}
