using Engine.Parser.FS;

using static Engine.Core.CoreModule;

namespace Engine.Sample;

internal class SampleRunner
{
    public static void Run(string text)
    {
        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(".", "ActiveCpuName"));
        var system = helper.TheSystem;
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
