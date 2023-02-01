namespace T.DriverIO

open NUnit.Framework

open Engine.Parser.FS
open T
open T.CPU
open System
open Engine.Core
open Engine.Common.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

[<AutoOpen>]
module DriverIOLSModuleTest =

    type DriverIOLSModule() =
        inherit EngineTestBaseClass()
        let t = CpuTestSample()
        do
            t.GenerationIO()

        [<Test>]
        member __.``Read Tag Flows`` () =
            for f in t.Flows do
            for tag in f.GetReadAbleTags() do
                //tag.Address 이용해서 읽기 테스트
                tag === tag

        [<Test>]
        member __.``Write Tag Flows`` () =
            for f in t.Flows do
            for tag in f.GetWriteAbleTags() do
                //tag.Address 이용해서 쓰기 테스트
                tag === tag

        [<Test>]
        member __.``Read Tag System`` () =
            for tag in t.Sys.GetReadAbleTags() do
                //tag.Address 이용해서 읽기 테스트
                tag === tag


        [<Test>]
        member __.``Write Tag System`` () =
            for tag in t.Sys.GetWriteAbleTags() do
                //tag.Address 이용해서 쓰기 테스트
                tag === tag

        [<Test>]
        member __.``Write Tag Reals`` () =
            () //calls, apis, reals 등등 추후 구현


