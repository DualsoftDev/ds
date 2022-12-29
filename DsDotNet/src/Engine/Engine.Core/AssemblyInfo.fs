module AssemblyInfo

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: InternalsVisibleTo("UnitTest.Engine")>]
[<assembly: InternalsVisibleTo("UnitTest.Expression")>]
[<assembly: InternalsVisibleTo("Engine.Parser.FS")>]
[<assembly: InternalsVisibleTo("Model.Import.Viewer")>]

do ()