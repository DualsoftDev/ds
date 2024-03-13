namespace CommonAssemblyInfo

open System.Reflection

#if DEBUG
[<assembly: AssemblyConfiguration("Debug")>]
#else
[<assembly: AssemblyConfiguration("Release")>]
#endif


[<assembly: AssemblyCompany("Dual INC.")>]
[<assembly: AssemblyCopyright("Copyright © Dual INC. 2016")>]
[<assembly: AssemblyCulture("")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyTitle("Dualsoft Engine packages")>]
[<assembly: AssemblyProduct("DS Engine")>]
[<assembly: AssemblyVersion($"0.9.5.2")>]


do ()
