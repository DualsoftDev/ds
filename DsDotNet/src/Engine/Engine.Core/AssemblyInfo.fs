module AssemblyInfo

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: InternalsVisibleTo("UnitTest.Engine")>]
[<assembly: InternalsVisibleTo("Engine.Parser.FS")>]

do ()