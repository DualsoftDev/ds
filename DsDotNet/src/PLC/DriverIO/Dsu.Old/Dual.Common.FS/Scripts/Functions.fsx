#I @"..\..\bin"
#I @"..\..\bin\netcoreapp3.1"
#I @"..\..\packages\FsUnit.xUnit.3.4.0\lib\net46"
#I @"..\..\Old.Dual.Common.xUnit.FS\bin\Debug"
#I @"..\..\packages\NHamcrest.2.0.1\lib\net451"

#r "FsUnit.Xunit.dll"
#r "NHamcrest.dll"
#r "Old.Dual.Common.FS.dll"
#r "Old.Dual.Common.xUnit.FS.dll"

// #load @"F:\solutions\GammaTest\soft\Gamma\Ds.Beta.UnitTest.FS\OnlyOnce.fs"
// #load loads script file
// #I __SOURCE_DIRECTORY__

open System
open System.IO
open Old.Dual.Common
open FsUnit
open Xunit
open Old.Dual.Common.UnitTest.FS

//let (===) x y = y |> should equal x


let add1 x y = x + y
let add2 x = fun y -> x + y
let add3 = fun x -> fun y -> x + y

add1 1 2 === 3
add2 1 2 === 3
add3 1 2 === 3


[1..3] |> List.map ((+) 1)
[[1..3]; [5..9]] |> List.map (List.map ((+) 1))

[[1..3]; [5..9]] |> List.mapInner ((+) 1)

let filterEven n = if n % 2 = 0 then Some n else None
[1..10] |> List.choose filterEven



//[1..10] |> List.chunkBySize 3;;
//val it : int list list = [[1; 2; 3]; [4; 5; 6]; [7; 8; 9]; [10]]

//[1..10] |> List.windowed 3;;
//val it : int list list =
//  [[1; 2; 3]; [2; 3; 4]; [3; 4; 5]; [4; 5; 6]; [5; 6; 7]; [6; 7; 8]; [7; 8; 9]; [8; 9; 10]]




/// System.Reactive 의 Observable.Window 를 참고해서 count 와 skip 을 구현
/// Sliding / Hopping window 지원
/// List.windowed 함수의 경우, skip 이 1 로 고정된 형태이고, count 갯수가 동일한 chunk 까지만 모으는데 반해,
/// windowed2 는 count 와 skip 을 받고, 맨 마지막 chunk 는 count 보다 작은 갯수를 허용한다.
/// Rx.NET in action.pdf, pp227
let rec windowed2 count skip (xs:'x list) =
    [
        if xs.Length >= count then
            yield xs |> List.take count
            yield! xs |> List.skip skip |> windowed2 count skip
        else
            yield xs
    ]

[1..10] |> windowed2 3 1;;
[1..10] |> windowed2 3 2;;
[1..10] |> windowed2 3 3;;
[1..10] |> windowed2 3 4;;


let testFunction x = function
    | Some(a) -> sprintf "%A: Some a" x
    | _ -> sprintf "%A: None" x


testFunction "X" (Some 1)



/// index 와 내용으로 fitering
let filteri (f:int->'t->bool) (xs: seq<'t>) =
    xs 
    |> Seq.indexed
    |> Seq.filter (fun tpl -> tpl ||> f)
    |> Seq.map snd


[0..10] |> filteri (fun i n -> i % 2 = 0 && n % 3 = 0) |> List.ofSeq
