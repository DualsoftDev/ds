namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Common.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

[<AutoOpen>]
module CpuTestUtil =

    let private getCpuTestSample () =
        let systemRepo   = ShareableSystemRepository ()
        let referenceDir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let sys = parseText systemRepo referenceDir Program.CpuTestText
        let flow = sys.Flows  |> Seq.head
        let real = flow.Graph.Vertices.OfType<Real>() |> Seq.last
        sys, flow, real

    let tReal = getCpuTestSample () |> fun(sys, flow, real) -> real
    let tCall = getCpuTestSample () |> fun(sys, flow, real) -> real.Graph.Vertices.OfType<Call>().First()
    let tFlow = getCpuTestSample () |> fun(sys, flow, real) -> flow 
    let tSys  = getCpuTestSample () |> fun(sys, flow, real) -> sys