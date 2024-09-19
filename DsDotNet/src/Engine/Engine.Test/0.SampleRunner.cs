using Antlr4.Runtime;

using Engine.Parser.FS;

using Microsoft.FSharp.Core;

using static Engine.Core.CoreModule;
using static Engine.Core.CoreModule.DeviceAndSystemModule;
using static Engine.Parser.FS.ParserOptionModule;

namespace Engine.Sample;

internal class SampleRunner
{
    public static void Run(string text)
    {
        var systemRepo = new Dictionary<string, DsSystem>();   // ShareableSystemRepository
        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(systemRepo, ".", "ActiveCpuName", FSharpOption<string>.None, ParserLoadingType.DuNone));
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
