// https://docs.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/

// References a package from NuGet
#r "nuget: FSharpPlus"

// #I : Specifies an assembly search path in quotation marks.
#I @"..\..\bin"
#I @"..\..\bin\netcoreapp3.1"
#I @"..\..\packages\FsUnit.xUnit.3.4.0\lib\net46"
#I @"..\..\Dual.Common.xUnit.FS\bin\Debug"
#I @"..\..\packages\NHamcrest.2.0.1\lib\net451"

// #r : References an assembly on disk
#r "FsUnit.Xunit.dll"
#r "NHamcrest.dll"
#r "Dual.Common.FS.dll"



#time

#load "test.fsx"