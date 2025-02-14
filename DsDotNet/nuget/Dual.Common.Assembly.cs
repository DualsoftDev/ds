using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;



[assembly: AssemblyCompany("Dual INC.")]
[assembly: AssemblyCopyright("Copyright © Dual INC. 2016")] // [저작권]
[assembly: AssemblyCulture("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyTitle("Dualsoft Nuget packages version = 파일버젼")]    // [파일 설명]


#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyProduct("(Debug) Dualsoft")]     // [제품 이름]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyProduct("Dualsoft")]     // [제품 이름]
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
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyInformationalVersion("0.9.1.0")]   // 제품 버젼 (탐색기 속성에서 볼 때)
//[assembly: AssemblyVersion("개별 따로 지정할 것")]    // 파일 버젼 : Nuget version 과 동일하게 유지
//[assembly: AssemblyFileVersion("0.9.1.0")]

