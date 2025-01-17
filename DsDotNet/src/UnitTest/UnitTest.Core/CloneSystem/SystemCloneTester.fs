namespace SystemCloneTests

open Xunit
open System.Linq
open FsUnit.Xunit
open Engine.Core
open Engine.Common

type DsSystemCloneTests() =

    let createTestSystem() =
        let system = DsSystem.Create4Test("OriginalSystem")
        let flow1 = system.CreateFlow("Flow1")
        let flow2 = system.CreateFlow("Flow2")
        system.Flows.Add(flow1) |> ignore
        system.Flows.Add(flow2) |> ignore

        let real1 = flow1.CreateReal("Real1")
        let real2 = flow2.CreateReal("Real2")
        let real3 = flow1.CreateReal("Real3")
        let real4 = flow2.CreateReal("Real4")

        // Setting members for Real instances
        real1.Motion <- Some "Motion1"
        real1.Script <- Some "Script1"
        real1.DsTime <- DsTime() // Assuming DsTime has a default constructor
        real1.Finished <- true
        real1.NoTransData <- true
        real1.IsSourceToken <- true

        real2.Motion <- Some "Motion2"
        real2.Script <- Some "Script2"
        real2.DsTime <- DsTime() // Assuming DsTime has a default constructor
        real2.Finished <- false
        real2.NoTransData <- false
        real2.IsSourceToken <- false

        // Adding edges between vertices
        flow1.CreateEdge(ModelingEdgeInfo<Vertex>(real1, ">", real3)) |> ignore
        flow2.CreateEdge(ModelingEdgeInfo<Vertex>(real2, ">", real4)) |> ignore

        Alias.Create("Alias1", DuAliasTargetReal real1, DuParentFlow flow2, false) |> ignore

        system

    [<Fact>]
    member _.``Test Clone System Name``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        clonedSystem.Name |> should equal "ClonedSystem"

    [<Fact>]
    member _.``Test Clone Flow Count``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        clonedSystem.Flows.Count |> should equal 2

    [<Fact>]
    member _.``Test Clone Real Count``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        clonedSystem.GetVertices().OfType<Real>().Count() |> should equal 4

    [<Fact>]
    member _.``Test Clone Alias Count``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        clonedSystem.GetVertices().OfType<Alias>().Count() |> should equal 1

    [<Fact>]
    member _.``Test Clone Flow Names``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        clonedSystem.Flows.ElementAt(0).Name |> should equal "Flow1"
        clonedSystem.Flows.ElementAt(1).Name |> should equal "Flow2"

    [<Fact>]
    member _.``Test Clone Real Names``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        let realNames = clonedSystem.GetVertices().OfType<Real>().Select(fun r -> r.Name).ToArray()
        realNames |> should contain "Real1"
        realNames |> should contain "Real2"
        realNames |> should contain "Real3"
        realNames |> should contain "Real4"

    [<Fact>]
    member _.``Test Clone Alias Name``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        clonedSystem.GetVertices().OfType<Alias>().ElementAt(0).Name |> should equal "Alias1"

    [<Fact>]
    member _.``Test Clone Real Members``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")

        let real1Clone = clonedSystem.GetVertices().OfType<Real>().First(fun r -> r.Name = "Real1")
        real1Clone.Motion |> should equal (Some "Motion1")
        real1Clone.Script |> should equal (Some "Script1")
        real1Clone.Finished |> should equal true
        real1Clone.NoTransData |> should equal true
        real1Clone.IsSourceToken |> should equal true

        let real2Clone = clonedSystem.GetVertices().OfType<Real>().First(fun r -> r.Name = "Real2")
        real2Clone.Motion |> should equal (Some "Motion2")
        real2Clone.Script |> should equal (Some "Script2")
        real2Clone.Finished |> should equal false
        real2Clone.NoTransData |> should equal false
        real2Clone.IsSourceToken |> should equal false

    [<Fact>]
    member _.``Test Clone Graph Structure in Flow1``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        let flow1 = clonedSystem.Flows.First(fun f -> f.Name = "Flow1")

        // Verify vertices in Flow1
        let flow1Vertices = flow1.Graph.Vertices.OfType<Real>().ToList()
        flow1Vertices.Count |> should equal 2
        flow1Vertices |> should contain (flow1Vertices.First(fun v -> v.Name = "Real1"))
        flow1Vertices |> should contain (flow1Vertices.First(fun v -> v.Name = "Real3"))

        // Verify edges in Flow1
        let flow1Edges = flow1.Graph.Edges.ToList()
        flow1Edges.Count |> should equal 1
        flow1Edges |> should contain (flow1Edges.First(fun e -> e.Source.Name = "Real1" && e.Target.Name = "Real3" && e.EdgeType = EdgeType.Start))

    [<Fact>]
    member _.``Test Clone Graph Structure in Flow2``() =
        let originalSystem = createTestSystem()
        let clonedSystem = originalSystem.Clone("ClonedSystem")
        let flow2 = clonedSystem.Flows.First(fun f -> f.Name = "Flow2")

        // Verify vertices in Flow2
        let flow2Vertices = flow2.Graph.Vertices.OfType<Real>().ToList()
        flow2Vertices.Count |> should equal 2
        flow2Vertices |> should contain (flow2Vertices.First(fun v -> v.Name = "Real2"))
        flow2Vertices |> should contain (flow2Vertices.First(fun v -> v.Name = "Real4"))

        // Verify edges in Flow2
        let flow2Edges = flow2.Graph.Edges.ToList()
        flow2Edges.Count |> should equal 1
        flow2Edges |> should contain (flow2Edges.First(fun e -> e.Source.Name = "Real2" && e.Target.Name = "Real4" && e.EdgeType = EdgeType.Start))
