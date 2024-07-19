namespace Engine.Tests

open NUnit.Framework
open Engine.Core
open System.Collections.Generic
open System.Linq

[<TestFixture>]
type DsSystemCloneTests() =

    let createTestSystem() =
        let system = DsSystem("OriginalSystem")
        let flow1 = Flow.Create("Flow1", system)
        let flow2 = Flow.Create("Flow2", system)
        system.Flows.Add(flow1) |> ignore
        system.Flows.Add(flow2) |> ignore

        let real1 = Real.Create("Real1", flow1)
        let real2 = Real.Create("Real2", flow2)
        let real3 = Real.Create("Real3", flow1)
        let real4 = Real.Create("Real4", flow2)

        // Setting members for Real instances
        real1.Motion <- Some "Motion1"
        real1.Script <- Some "Script1"
        real1.RealData <- 42uy
        real1.DsTime <- DsTime() // Assuming DsTime has a default constructor
        real1.Finished <- true
        real1.NoTransData <- true

        real2.Motion <- Some "Motion2"
        real2.Script <- Some "Script2"
        real2.RealData <- 24uy
        real2.DsTime <- DsTime() // Assuming DsTime has a default constructor
        real2.Finished <- false
        real2.NoTransData <- false

        // Adding edges between vertices
        flow1.CreateEdge(ModelingEdgeInfo<Vertex>(real1 , ">", real3)) |> ignore
        flow2.CreateEdge(ModelingEdgeInfo<Vertex>(real2 , ">", real4)) |> ignore
        
        Alias.Create("Alias1", DuAliasTargetReal real1, DuParentFlow flow2, false) |> ignore

        system

    [<Test>]
    member _.``Test Clone System Name``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        Assert.AreEqual("ClonedSystem", clonedSystem.Name)

    [<Test>]
    member _.``Test Clone Flow Count``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        Assert.AreEqual(2, clonedSystem.Flows.Count)

    [<Test>]
    member _.``Test Clone Real Count``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        Assert.AreEqual(4, clonedSystem.GetVertices().OfType<Real>().Count())

    [<Test>]
    member _.``Test Clone Alias Count``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        Assert.AreEqual(1, clonedSystem.GetVertices().OfType<Alias>().Count())

    [<Test>]
    member _.``Test Clone Flow Names``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        Assert.AreEqual("Flow1", clonedSystem.Flows.ElementAt(0).Name)
        Assert.AreEqual("Flow2", clonedSystem.Flows.ElementAt(1).Name)

    [<Test>]
    member _.``Test Clone Real Names``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        let realNames = clonedSystem.GetVertices().OfType<Real>().Select(fun r -> r.Name).ToArray()
        Assert.Contains("Real1", realNames)
        Assert.Contains("Real2", realNames)
        Assert.Contains("Real3", realNames)
        Assert.Contains("Real4", realNames)

    [<Test>]
    member _.``Test Clone Alias Name``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        Assert.AreEqual("Alias1", clonedSystem.GetVertices().OfType<Alias>().ElementAt(0).Name)

    [<Test>]
    member _.``Test Clone Real Members``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")

        let real1Clone = clonedSystem.GetVertices().OfType<Real>().First(fun r -> r.Name = "Real1")
        Assert.AreEqual(Some "Motion1", real1Clone.Motion)
        Assert.AreEqual(Some "Script1", real1Clone.Script)
        Assert.AreEqual(42uy, real1Clone.RealData)
        Assert.AreEqual(true, real1Clone.Finished)
        Assert.AreEqual(true, real1Clone.NoTransData)
        
        let real2Clone = clonedSystem.GetVertices().OfType<Real>().First(fun r -> r.Name = "Real2")
        Assert.AreEqual(Some "Motion2", real2Clone.Motion)
        Assert.AreEqual(Some "Script2", real2Clone.Script)
        Assert.AreEqual(24uy, real2Clone.RealData)
        Assert.AreEqual(false, real2Clone.Finished)
        Assert.AreEqual(false, real2Clone.NoTransData)

    [<Test>]
    member _.``Test Clone Graph Structure in Flow1``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        let flow1 = clonedSystem.Flows.First(fun f -> f.Name = "Flow1")
        
        // Verify vertices in Flow1
        let flow1Vertices = flow1.Graph.Vertices.OfType<Real>().ToList()
        Assert.AreEqual(2, flow1Vertices.Count)
        Assert.IsTrue(flow1Vertices.Any(fun v -> v.Name = "Real1"))
        Assert.IsTrue(flow1Vertices.Any(fun v -> v.Name = "Real3"))

        // Verify edges in Flow1
        let flow1Edges = flow1.Graph.Edges.ToList()
        Assert.AreEqual(1, flow1Edges.Count)
        Assert.IsTrue(flow1Edges.Any(fun e -> e.Source.Name = "Real1" && e.Target.Name = "Real3" && e.EdgeType = EdgeType.Start))

    [<Test>]
    member _.``Test Clone Graph Structure in Flow2``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        let flow2 = clonedSystem.Flows.First(fun f -> f.Name = "Flow2")

        // Verify vertices in Flow2
        let flow2Vertices = flow2.Graph.Vertices.OfType<Real>().ToList()
        Assert.AreEqual(2, flow2Vertices.Count)
        Assert.IsTrue(flow2Vertices.Any(fun v -> v.Name = "Real2"))
        Assert.IsTrue(flow2Vertices.Any(fun v -> v.Name = "Real4"))

        // Verify edges in Flow2
        let flow2Edges = flow2.Graph.Edges.ToList()
        Assert.AreEqual(1, flow2Edges.Count)
        Assert.IsTrue(flow2Edges.Any(fun e -> e.Source.Name = "Real2" && e.Target.Name = "Real4" && e.EdgeType = EdgeType.Start))
