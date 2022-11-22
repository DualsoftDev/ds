namespace UnitTest.Engine


open System.Linq
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS

[<AutoOpen>]
module ModelBuildupTests1 =

    type Buildup() =
        inherit TestBase()
        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let compare = compare libdir
        let compareExact x = compare x x

        let createSimpleSystem() =
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
            system, flow, real, callAp, callAm

        [<Test>]
        member __.``Model creation test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = VertexCall.Create("Ap", callAp, Real real)
            let vCallM = VertexCall.Create("Am", callAm, Real real)
            real.CreateEdge(ModelingEdgeInfo(vCallP, ">", vCallM)) |> ignore

            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
        Main = {
            Ap > Am;
        }
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [device file="cylinder.ds"] A;
}"""
            logDebug $"{generated}"
            compare generated answer
            ()

        [<Test>]
        member __.``Invalid Model creation test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = VertexCall.Create("Ap", callAp, Real real)
            let vCallM = VertexCall.Create("Am", callAm, Real real)
            ( fun () ->
                // real 의 child 간 edge 를 flow 에서 생성하려 함.. should fail
                flow.CreateEdge(ModelingEdgeInfo(vCallP, ">", vCallM)) |> ignore
            ) |> ShouldFailWithSubstringT "not child of"

        [<Test>]
        member __.``XModel with alias test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = VertexAlias.Create("Main2", AliasTargetReal real, Flow flow)
            let real2 = Real.Create("R2", flow)

            flow.CreateEdge(ModelingEdgeInfo(vCallP, ">", real2)) |> ignore
            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
        Main2 > R2;		// Main2(VertexAlias)> R2(Real);
        Main; // island
        // todo : alias 가 생성 안됨.
        [aliases] = {
            Main = { Main2; }
        }
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [device file="cylinder.ds"] A;
}
"""
            logDebug $"{generated}"
            compare generated answer
