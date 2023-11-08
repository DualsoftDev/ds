```fs
namespace CommonAsssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyCompany("Dual INC.")>]
[<assembly: AssemblyCopyright("Copyright © Dual INC. 2016")>]
[<assembly: AssemblyCulture("")>]
[<assembly: AssemblyTrademark("")>]


#if DEBUG
[<assembly: AssemblyConfiguration("Debug")>]
#else
[<assembly: AssemblyConfiguration("Release")>]
#endif




// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [<assembly: AssemblyVersion("1.0.*")>]
[<assembly: AssemblyInformationalVersion("0.9.1.0")>]
[<assembly: AssemblyVersion("0.9.1.0")>]
//[<assembly: AssemblyFileVersion("0.9.1.0")>]



do
    ()
```