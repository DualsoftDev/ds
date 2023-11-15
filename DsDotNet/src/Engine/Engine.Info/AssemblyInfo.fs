module AssemblyInfo

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: InternalsVisibleTo("UnitTest.Engine")>]
[<assembly: InternalsVisibleTo("Newtonsoft.Json")>]
[<assembly: InternalsVisibleTo("DsWebApp.Server")>]

do ()
