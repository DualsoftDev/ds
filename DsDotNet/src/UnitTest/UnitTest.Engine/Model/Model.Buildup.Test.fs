namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module ModelBuildupTests1 =

    type Buildup() =
        inherit TestBase()
        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let compare = compare libdir
        let compareExact x = compare x x
        [<Test>]
        member __.``Model creation test`` () =
            let system = DsSystem.Create("My", "localhost")
            let flow = Flow.Create("F", system)
            let real = Real.Create("Main", flow)
            let a = system.LoadDeviceAs("A", @$"{libdir}\cylinder.ds", "cylinder.ds")

            let apis = a.ReferenceSystem.ApiItems4Export
            let apiP = apis.First(fun ai -> ai.Name = "+")
            let apiM = apis.First(fun ai -> ai.Name = "-")
            let callAp =
                let apiItem = ApiItem(apiP, "%Q1", "%I1")
                Call("Ap", [apiItem])
            let callAm =
                let apiItem = ApiItem(apiM, "%Q2", "%I2")
                Call("Am", [apiItem])
            system.Calls.AddRange([callAp; callAm])

            let vCallP = VertexCall.Create("Ap", callAp, Real real)
            let vCallM = VertexCall.Create("Am", callAm, Real real)
            flow.CreateEdges(ModelingEdgeInfo(vCallP, ">", vCallM)) |> ignore
            ()

            //let vertexs = HashSet<Real>()
            //let find(name:string) = vertexs.First(fun f->f.Name = name)
            //for v in [
            //            "START"; "시작인과"; "시작유지"; "RESET"; "복귀인과";
            //            "복귀유지"; "ETC"; "상호행위간섭"; "시작후행리셋";
            //    ] do
            //        vertexs.Add(Real.Create(v, flow)) |>ignore

            //let fg = flow.Graph
            //fg.AddVertices(vertexs.Cast<Vertex>())|>ignore
            //let v(name:string) = fg.Vertices.Find(fun f->f.Name = name)

            //flow.ModelingEdges.Add(ModelingEdgeInfo(v("START"), TextStartEdge, v("시작인과")))|>ignore
            //flow.ModelingEdges.Add(ModelingEdgeInfo(v("START"), TextStartPush, v("시작유지")))|>ignore
            //flow.ModelingEdges.Add(ModelingEdgeInfo(v("RESET"), TextResetEdge, v("복귀인과")))|>ignore
            //flow.ModelingEdges.Add(ModelingEdgeInfo(v("RESET"), TextResetPush, v("복귀유지")))|>ignore
            //flow.ModelingEdges.Add(ModelingEdgeInfo(v("ETC"), TextStartEdge, v("상호행위간섭")))|>ignore
            //flow.ModelingEdges.Add(ModelingEdgeInfo(v("ETC"), TextStartReset, v("시작후행리셋")))|>ignore
