module AssemblyInfo

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: InternalsVisibleTo("UnitTest.Core")>]
[<assembly: InternalsVisibleTo("UnitTest.Engine")>]
[<assembly: InternalsVisibleTo("UnitTest.Expression")>]
[<assembly: InternalsVisibleTo("Engine.Parser.FS")>]
[<assembly: InternalsVisibleTo("Engine.CodeGenCPU")>]
[<assembly: InternalsVisibleTo("Model.Import.Viewer")>]
[<assembly: InternalsVisibleTo("PowerPointAddInHelper")>]
[<assembly: InternalsVisibleTo("Dualsoft")>]
[<assembly: InternalsVisibleTo("TestProfiler")>]
[<assembly: InternalsVisibleTo("Newtonsoft.Json")>]

do ()