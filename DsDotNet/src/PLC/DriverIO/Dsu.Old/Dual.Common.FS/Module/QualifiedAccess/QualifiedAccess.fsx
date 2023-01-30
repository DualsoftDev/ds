open System.Collections.Generic

#I @"F:\solutions\GammaTest\soft\Gamma\bin"
#I @"F:\solutions\GammaTest\soft\Gamma\Ds.Beta.UnitTest.FS\bin\Debug"
#I @"F:\solutions\GammaTest\soft\Gamma\Old.Dual.Common.xUnit.FS\bin\Debug"
#r "FsUnit.Xunit.dll"
#r "NHamcrest.dll"
#r "Old.Dual.Common.FS.dll"
#r "Old.Dual.Common.xUnit.FS.dll"
//#load @"F:\solutions\GammaTest\soft\Gamma\Ds.Beta.UnitTest.FS\OnlyOnce.fs"
// #load loads script file
//#I __SOURCE_DIRECTORY__

open System
open System.IO
open Old.Dual.Common
open FsUnit
open Xunit
open Old.Dual.Common.UnitTest.FS
open System.Collections.Generic

module private SeqTest =
    let testMe() =
        [[1; 2; 3; 5]; [2; 3; 5]; [1; 4; 5]] |> List.intersectMany === [5]

        [1..10] |> List.differencePipe [3..10] === [1;2]
        [1..10] |> Seq.containsAllOfPipe [3..5]        |> SbTrue
        [1..10] |> Seq.containsAllOfPipe (100::[3..5]) |> SbFalse
        [1..10] |> Seq.containsAnyOfPipe (100::[3..5]) |> SbTrue

module TupleTest =
    let a = (1,2,3) |> Tuple.toSeq |> Seq.cast<int>
    (1,2,3) |> Tuple.toSeq |> Seq.cast<int> |> List.ofSeq === [1; 2; 3]
    [1..3] |> List.map box |> Tuple.ofSeq === (1, 2, 3)

module TestMap =
    let testMe() =
        let map1 = [ (3, "셋"); (4, "넷"); (5, "다섯")] |> Map.ofSeq
        let keys = map1.Keys |> List.ofSeq

        let boxSnd(k, v) = k, box(v)
        let dic1 = [ (3, 3); (1, 1); (2, 2); ]               |> Seq.map boxSnd |> Map.ofSeq
        let dic2 = [ (3, "Three"); (4, "Four"); (5, "Five")] |> Seq.map boxSnd |> Map.ofSeq
        let dic3 = [ (3, "셋"); (4, "넷"); (5, "다섯")]       |> Seq.map boxSnd |> Map.ofSeq

        findAll 3 [dic1; dic2; dic3] |> List.ofSeq === [box 3; box "Three"; box "셋"]
        tryFind 6 [dic1; dic2; dic3] |> SbNone
        (fun () -> find 6 [dic1; dic2; dic3] |> ignore) |> ShouldFail

    // 다중 중복 키 허용 dictionary 는 MultiValueDictionary 를 이용
    // Microsoft.Collections.Extensions.MultiValueDictionary

    let testMultimap() =
        let data = [
            0, "zero"
            0, "none"
            0, "empty"
            1, "one"
            2, "two"
        ]

        let mmap = data |> MultiMap.CreateFlat
        let z = mmap.[0]
        assert(z.Contains("zero"))
        assert(z.Contains("none"))
        assert(z.Contains("empty"))
        let a1 = mmap.ContainsKeyAndValue(0, "zero")
        let a2 = mmap.ContainsKeyAndValue(0, "none")
        let a3 = mmap.ContainsKeyAndValue(0, "empty")
        let a4 = mmap.ContainsKeyAndValue(1, "one")

        let e1 = mmap.ContainsKeyAndValue(0, "xnone")
        let e4 = mmap.ContainsKeyAndValue(1, "none")

        let a = [1, "one"] |> dict
        let dic = Dictionary(a)
        ()

