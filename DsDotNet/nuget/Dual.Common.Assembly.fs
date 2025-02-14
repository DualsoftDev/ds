namespace CommonAsssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyCompany("Dual INC.")>]
[<assembly: AssemblyCopyright("Copyright © Dual INC. 2016")>]
[<assembly: AssemblyCulture("")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyTitle("Dualsoft Nuget packages")>]  // 파일 설명
[<assembly: AssemblyInformationalVersion("0.9")>]    // 제품 버젼 (탐색기 속성에서 볼 때)


#if DEBUG
[<assembly: AssemblyConfiguration("Debug")>]
[<assembly: AssemblyProduct("(DEBUG) Nuget Package version 은 파일 버젼과 동일")>]  // 제품 이름
#else
[<assembly: AssemblyConfiguration("Release")>]
[<assembly: AssemblyProduct("Nuget Package version 은 파일 버젼과 동일")>]  // 제품 이름
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
//[<assembly: AssemblyVersion("개별 따로 지정할 것")>]    // 파일 버젼
//[<assembly: AssemblyVersion("1.0.*")>]
//[<assembly: AssemblyFileVersion("0.9.1.0")>]



do
    ()