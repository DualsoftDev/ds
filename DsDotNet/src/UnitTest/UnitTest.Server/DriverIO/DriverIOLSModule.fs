namespace T.DriverIO

open NUnit.Framework

open Engine.Parser.FS
open T
open T.CPU
open System
open Engine.Core
open Dual.Common.Core.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

[<AutoOpen>]
module DriverIOLSModuleTest =

    type DriverIOLSModule() =
        inherit EngineTestBaseClass()
        let t = CpuTestSample()

        [<Test>]
        member __.``Get TagInfo`` () =
            for tag in t.Sys.Storages.Values do
                match tag.GetTagInfo() with
                | Some t ->
                    match t.TagTarget with
                    |TTSystem   -> t.TagSystem.Value  |> fun (sys:DsSystem,tag:SystemTag)  -> () //관련 Ds Server 작업
                    |TTFlow     -> t.TagFlow.Value    |> fun (sys:Flow,    tag:FlowTag)    -> () //관련 Ds Server 작업
                    |TTVertex   -> t.TagVertex.Value  |> fun (sys:Vertex,  tag:VertexTag)  -> () //관련 Ds Server 작업
                    |TTApiItem  -> t.TagApiItem.Value |> fun (sys:ApiItem, tag:ApiItemTag) -> () //관련 Ds Server 작업

                | None -> ()    //Ds Server와 관련없는 Timmer 등등.. GetTagInfo None

                tag === tag

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


