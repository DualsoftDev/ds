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
#r "FSharpx.Collections.Experimental.dll"

// #load loads script file
// #load @"..\..\Dual.Common.xUnit.FS\OnlyOnce.fs"
// #I __SOURCE_DIRECTORY__

(*
__LINE__
__SOURCE_DIRECTORY__
__SOURCE_FILE__
*)


open System
open System.IO
open Dual.Common
open FsUnit
open Xunit
open Dual.Common.UnitTest.FS

let add1 x y = x + y
let add2 x = fun y -> x + y
let add3 = fun x -> fun y -> x + y

add1 1 2 === 3
add2 1 2 === 3
add3 1 2 === 3


[1..3] |> List.map ((+) 1)
[[1..3]; [5..9]] |> List.map (List.map ((+) 1))

[[1..3]; [5..9]] |> List.mapInner ((+) 1)
