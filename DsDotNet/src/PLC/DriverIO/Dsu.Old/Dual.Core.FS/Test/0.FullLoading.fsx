// "F:\Git\dual\soft\Delta\Dual.Common.FS\Scripts" 에서의 상대 경로
//                         < .. >          <.>  

#I @"..\..\bin"
#I @"..\..\packages\FsUnit.xUnit.3.4.0\lib\net46"
#I @"..\..\Dual.Common.xUnit.FS\bin\Debug"
#I @"..\..\packages\NHamcrest.2.0.1\lib\net451"
#I @"..\..\packages\FSharpx.Collections.Experimental.1.7.3\lib\40"
#r "FsUnit.Xunit.dll"
#r "NHamcrest.dll"
#r "Dual.Common.FS.dll"
#r "Dual.Common.xUnit.FS.dll"
#r "Dual.Core.FS.dll"
#r "FSharpx.Collections.Experimental.dll"

// #load loads script file
// #load @"..\..\Dual.Common.xUnit.FS\OnlyOnce.fs"
// #I __SOURCE_DIRECTORY__

// #load "0.FullLoading.fsx"

open System
open System.IO
open Dual.Common
open FsUnit
open Xunit
open Dual.Common.UnitTest.FS

open Dual.Core
open Dual.Core.Types

