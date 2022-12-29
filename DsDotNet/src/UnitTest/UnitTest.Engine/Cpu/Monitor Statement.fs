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

type MonitorStatement() =
    do Fixtures.SetUpTest()

    let getCpuTestSample () =
        let systemRepo   = ShareableSystemRepository ()
        let referenceDir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let sys = parseText systemRepo referenceDir Program.CpuTestText
        let flow = sys.Flows  |> Seq.head
        let real = flow.Graph.Vertices.OfType<Real>() |> Seq.head
        sys, flow, real

    [<Test>]
    member __.``M1 Origin Monitor`` () =
        let sys, flow, real = getCpuTestSample()
        let realM = real.VertexManager :?> VertexManager
        let test = realM.M1_OriginMonitor()
        Eq  1 1 

    [<Test>] member __.``M2 Pause Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M3 Call Error TX Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M4 Call Error RX Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M5 Real Error RX Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M6 Real Error RX Monitor`` () =  Eq 1 1 //test ahn
    
       
          