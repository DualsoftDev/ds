namespace T

open Dual.UnitTest.Common.FS
open Engine.Core
open NUnit.Framework
open System.Collections.Generic

[<AutoOpen>]
module TimeTestModule =

    type TimeTest() =

        let system = DsSystem.Create4Test("My")
        let flow = Flow.Create("F", system)

        let createVertex name time =
            let r = Real.Create(name, flow) :> Vertex
            r.Time <- Some time
            r

        let createEdge source target =
            Edge.Create(flow.Graph, source, target, EdgeType.Start) |> ignore

        let initializeGraph() =
            // DAG Group 1
            let v0 = createVertex "V0" 0
            let v1 = createVertex "V1" 1
            let v2 = createVertex "V2" 2
            let v3 = createVertex "V3" 3
            let v4 = createVertex "V4" 4
            let v5 = createVertex "V5" 5
            let v6 = createVertex "V6" 6

            createEdge v0 v1
            createEdge v1 v2
            createEdge v1 v4
            createEdge v2 v3
            createEdge v3 v5
            createEdge v4 v3
            createEdge v0 v4
            createEdge v4 v5

            // DAG Group 2
            let v7 = createVertex "V7" 7
            let v8 = createVertex "V8" 8
            let v9 = createVertex "V9" 9
            let v10 = createVertex "V10" 10
            let v11 = createVertex "V11" 0
            let v12 = createVertex "V12" 0

            createEdge v7 v8
            createEdge v8 v9
            createEdge v9 v10
            createEdge v10 v11
            createEdge v11 v12 

            v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12

        [<Test>]
        member _.``Single Source Single Target Test Group 1`` () =
            let v0, v1, _, _, _, _, _, _, _, _, _, _, _ = initializeGraph()

            // Test GetDuration with a single source and single target in Group 1
            let duration = TimeExt.GetDuration(flow.Graph, v0, v1)

            // Verify the result
            Assert.AreEqual(Some 1, duration) // Expected duration: V0 -> V1 = 0 + 1

        [<Test>]
        member _.``Single Source Single Target Test Group 2`` () =
            let _, _, _, _, _, _, _, v7, _, _, _, v11, v12 = initializeGraph()

            // Test GetDuration with a single source and single target in Group 2
            let duration = TimeExt.GetDuration(flow.Graph, v7, v12)

            // Verify the result
            Assert.AreEqual(Some 34, duration) // Expected duration: V7 -> V8 -> V9 -> V10 -> V11 -> V12 = 7 + 8 + 9 + 10 + 0 + 0

        [<Test>]
        member _.``Multiple Paths Test Group 1`` () =
            let v0, v1, v2, v3, v4, v5, _, _, _, _, _, _, _ = initializeGraph()

            // Test GetDuration with multiple paths in Group 1
            let duration = TimeExt.GetDuration(flow.Graph, v0, v5)

            // Verify the result
            Assert.AreEqual(Some 13, duration) // Expected duration: V0 -> V1 -> V4 -> V3 -> V5 = 0 + 1 + 4 + 3 + 5

        [<Test>]
        member _.``Disconnected Path Test Group 1`` () =
            let v0, _, _, _, _, _, _, v7, _, _, _, _, _ = initializeGraph()

            // Test GetDuration with a disconnected path
            let duration = TimeExt.GetDuration(flow.Graph, v0, v7)

            // Verify the result
            Assert.AreEqual(None, duration) // Expected duration: None (no path from v0 to v7)

        [<Test>]
        member _.``Complex Path Test Group 1`` () =
            let v0, v1, v2, v3, v4, v5, _, _, _, _, _, _, _ = initializeGraph()

            // Test GetDuration with a single source and single target in a complex path
            let duration = TimeExt.GetDuration(flow.Graph, v3, v4)

            // Verify the result
            Assert.AreEqual(None, duration)

        [<Test>]
        member _.``Complex Path Test Group 2`` () =
            let _, _, _, _, _, _, v6, _, v7, v8, v9, v10, v11 = initializeGraph()

            // Test GetDuration with a single source and single target in a complex path
            let duration = TimeExt.GetDuration(flow.Graph, v6, v11)

            // Verify the result
            Assert.AreEqual(None, duration)

        [<Test>]
        member _.``Longer Path Test Group 1`` () =
            let v0, _, _, _, _, _, v6, _, _, _, _, _, v12 = initializeGraph()

            // Test GetDuration with a longer path in Group 1
            let duration = TimeExt.GetDuration(flow.Graph, v0, v12)

            // Verify the result
            Assert.AreEqual(None, duration)

        [<Test>]
        member _.``Test with No Path`` () =
            let v0, _, _, _, _, _, v6, _, _, _, _, _, _ = initializeGraph()

            // Test GetDuration with no path from source to target
            let duration = TimeExt.GetDuration(flow.Graph, v0, v6)

            // Verify the result
            Assert.AreEqual(None, duration) // Expected duration: None (no path from v0 to v6)

        [<Test>]
        member _.``Test with Self Loop`` () =
            let v0, _, _, _, _, _, _, _, _, _, _, _, _ = initializeGraph()

            // Test GetDuration with a self loop
            let duration = TimeExt.GetDuration(flow.Graph, v0, v0)

            // Verify the result
            Assert.AreEqual(Some 0, duration) // Expected duration: Self loop, duration 0

        [<Test>]
        member _.``Complex Path with All Nodes`` () =
            let v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12 = initializeGraph()

            // Test GetDuration with a complex path that includes all nodes
            let duration = TimeExt.GetDuration(flow.Graph, v0, v4)

            // Verify the result
            Assert.AreEqual(Some 5, duration) // Expected duration: V0 ->  V1 -> V4 = 0 + 1 + 4

        [<Test>]
        member _.``Complex Path with Partial Nodes`` () =
            let v0, v1, v2, v3, v4, v5, _, _, v7, v8, v9, v10, _ = initializeGraph()

            // Test GetDuration with a complex path that includes partial nodes
            let duration = TimeExt.GetDuration(flow.Graph, v0, v9)

            // Verify the result
            Assert.AreEqual(None, duration) // Expected duration: None (no path from v0 to v9)

        [<Test>]
        member _.``Single Source Single Target Test Group 3`` () =
            let _, _, _, _, _, _, _, _, _, _, _, v11, v12 = initializeGraph()

            // Test GetDuration with a single source and single target in Group 2
            let duration = TimeExt.GetDuration(flow.Graph, v11, v12)

            // Verify the result
            Assert.AreEqual(Some 0, duration) // Expected duration: V11 -> V12 = 0 + 0


        [<Test>]
        member _.``Multiple Sources Multiple Targets Test 1`` () =
            let v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12 = initializeGraph()

            // Test GetDuration with multiple sources and multiple targets
            let duration = TimeExt.GetDuration(flow.Graph, [v0; v7], [v5; v12])

            // Verify the result
            Assert.AreEqual(Some 34, duration) // Expected duration: Longest path from any source to any target

        [<Test>]
        member _.``Multiple Sources Multiple Targets Test 2`` () =
            let v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12 = initializeGraph()

            // Test GetDuration with multiple sources and multiple targets
            let duration = TimeExt.GetDuration(flow.Graph, [v1; v7], [v3; v12])

            // Verify the result
            Assert.AreEqual(Some 34, duration) // Expected duration: Longest path from any source to any target

        [<Test>]
        member _.``Multiple Sources Multiple Targets Test 3`` () =
            let v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12 = initializeGraph()

            // Test GetDuration with multiple sources and multiple targets
            let duration = TimeExt.GetDuration(flow.Graph, [v0; v1], [v4; v5])

            // Verify the result
            Assert.AreEqual(Some 13, duration) // Expected duration: Longest path from any source to any target, satisfying all targets
