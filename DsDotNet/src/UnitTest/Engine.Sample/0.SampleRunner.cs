using Antlr4.Runtime;

using Engine.Parser.FS;

using Microsoft.FSharp.Core;

using static Engine.Core.CoreModule;
using static Engine.Core.CoreDevicesModule;
using static Engine.Core.CoreModule.SystemModule;
using static Engine.Parser.FS.ParserOptionModule;
using static Engine.Core.Interface;
using System.Collections.Generic;

namespace Engine.Sample;

internal class SampleRunner
{
    public static void Run(string text)
    {
        var systemRepo = new Dictionary<string, ISystem>();   // ShareableSystemRepository
        var system = ModelParser.ParseFromString(text, ParserOptions.Create4Simulation(systemRepo, ".", "ActiveCpuName", FSharpOption<string>.None, ParserLoadingType.DuNone));
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
