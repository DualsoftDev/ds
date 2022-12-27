namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU

type MonitorTest() =
    do Fixtures.SetUpTest()

    let getCpuTestSample () =
        let systemRepo   = ShareableSystemRepository ()
        let referenceDir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let sys = parseText systemRepo referenceDir Program.CpuTestText
        let flow = sys.Flows  |> Seq.head
        let real = flow.Graph.Vertices.GetReals() |> Seq.head
        sys, flow, real

    [<Test>]
    member __.``1 Origin Monitor`` () =
        let sys, flow, real = getCpuTestSample()
        let realM = real.VertexMemoryManager :?> VertexMemoryManager
        let test = realM.M1_OriginMonitor()
        Eq  1 1 

    [<Test>]
    member __.``2 Pause Monitor`` () =
        Eq 1 1
          