using Engine.Parser;

using static Engine.Core.CoreModule;

namespace Engine.Sample;

internal class SampleRunner
{
    public static void Run(string text)
    {
        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation());
        var model = helper.Model;
        //Try("1 + 2 + 3");
        //Try("1 2 + 3");
        //Try("1 + +");
        foreach (var kv in helper._elements)
        {
            var (p, type_) = (kv.Key, kv.Value);
            var types = type_.ToString("F");
            Trace.WriteLine(p.Combine("/") + $":{types}");
        }

        Trace.WriteLine("---- Spit result");
        var spits = model.Spit();
        foreach (var spit in spits)
        {
            var tName = spit.Obj.GetType().Name;
            var name = spit.NameComponents.Combine();
            Trace.WriteLine($"{name}:{tName}");
        }

        var spitObjs = spits.Select(spit => spit.Obj);
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
}";

}
