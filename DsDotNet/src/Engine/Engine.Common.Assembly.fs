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


///DS Library Version (Library File 수정시만 변경 Ver : 년.월.시.0)
[<assembly: AssemblyDescription("Library Release Date 24.3.26")>]
///DS Language Version (Language Parser 수정시만 변경 Ver : 1.0.0.1)
[<assembly: AssemblyFileVersion($"1.0.0.1")>]
///DS Engine Version
[<assembly: AssemblyVersion($"0.9.7.10")>]
do ()
