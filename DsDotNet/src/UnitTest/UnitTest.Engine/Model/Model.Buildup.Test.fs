namespace UnitTest.Engine


open System.Linq
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS

[<AutoOpen>]
module ModelBuildupTests1 =

    type Buildup() =
        do
            Fixtures.SetUpTest()

        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let compare = compare libdir
        let compareExact x = compare x x

        let createSimpleSystem() =
            let system = DsSystem("My", "localhost")
            let flow = Flow.Create("F", system)
            let real = Real.Create("Main", flow)
            let a = system.LoadDeviceAs("A", @$"{libdir}\cylinder.ds", "cylinder.ds")

            let apis = system.ApiUsages
            let apiP = apis.First(fun ai -> ai.Name = "+")
            let apiM = apis.First(fun ai -> ai.Name = "-")
            let callAp =
                let apiItem = ApiCallDef(apiP, "%Q1", "%I1")
                Call("Ap", [apiItem])
            let callAm =
                let apiItem = ApiCallDef(apiM, "%Q2", "%I2")
                Call("Am", [apiItem])
            system.Calls.AddRange([callAp; callAm])
            system, flow, real, callAp, callAm

        [<Test>]
        member __.``Model creation test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = VertexCall.Create("Ap", callAp, Real real)
            let vCallM = VertexCall.Create("Am", callAm, Real real)
            real.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, ">", vCallM)) |> ignore

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
}
"""
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
                flow.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, ">", vCallM)) |> ignore
            ) |> ShouldFailWithSubstringT "not child of"

        [<Test>]
        member __.``Model with alias test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = VertexAlias.Create("Main2", AliasTargetReal real, Flow flow)
            let real2 = Real.Create("R2", flow)

            flow.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, ">", real2)) |> ignore
            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
        Main2 > R2;		// Main2(VertexAlias)> R2(Real);
        Main; // island
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


        [<Test>]
        member __.``Model with other flow real call test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let flow2 = Flow.Create("F2", system)

            let real2 = VertexOtherFlowRealCall.Create(real, Flow flow2)
            let real3 = Real.Create("R3", flow2)

            flow2.CreateEdge(ModelingEdgeInfo<Vertex>(real2, ">", real3)) |> ignore
            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
    }
    [flow] F2 = {
        F.Main > R3;		// F.Main(VertexOtherFlowRealCall)> R3(Real);
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



        [<Test>]
        member __.``Model with export api test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()
            let real2 = Real.Create("Main2", flow)
            let adv = ApiInterface.Create("Adv", system, [real], [real])
            let ret = ApiInterface.Create("Ret", system, [real2], [real2])
            [ adv; ret; ].Iter(system.ApiInterfaces.Add >> ignore)

            ApiResetInfo.Create(system, "Adv", ModelingEdgeType.Interlock, "Ret") |> ignore

            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
            Main2; // island
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [interfaces] = {
        Adv = { F.Main ~ F.Main }
        Ret = { F.Main2 ~ F.Main2 }
        Adv <||> Ret;
    }
    [device file="cylinder.ds"] A;
}
"""
            logDebug $"{generated}"
            compare generated answer



        [<Test>]
        member __.``Model with buttons test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            system.AddButton(BtnType.EmergencyBTN, "STOP", flow)
            system.AddButton(BtnType.StartBTN, "START", flow)

            let flow2 = Flow.Create("F2", system)
            system.AddButton(BtnType.EmergencyBTN, "STOP2", flow2)
            system.AddButton(BtnType.StartBTN, "START2", flow2)

            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
    }
    [flow] F2 = {
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [emg] = {
        STOP = { F; }
        STOP2 = { F2; }
    }
    [start] = {
        START = { F; }
        START2 = { F2; }
    }
    [device file="cylinder.ds"] A;
}
"""
            logDebug $"{generated}"
            compare generated answer





        [<Test>]
        member __.``Model with code element test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let v = CodeElements.Variable
            let c = CodeElements.Command
            let o = CodeElements.Observe
            [
                v("R100", "word", "0")
                v("R101", "word", "1")
                v("R102", "int", "1")
            ] |> system.Variables.AddRange

            (*
                [commands] = {
                    CMD1 = (@Delay = 0)
                    CMD2 = (@Delay = 30)
                    CMD3 = (@add = 30, 50 ~ R103)  //30+R101 = R103
                }
            *)
            let fa = FunctionApplication
            [
                c("CMD1", fa("Delay", [| [|"0"|] |]))
                c("CMD2", fa("Delay", [| [|"30"|] |]))
                c("CMD2", fa("add",   [| [|"30"; "50"|]; [|"R103"|] |]))
            ] |> system.Commands.AddRange

            (*
                [observes] = {
                    CON1 = (@GT = R102, 5)
                    CON2 = (@Delay = 30)
                    CON3 = (@Not = Tag1)
                }
            *)
            [
                o ("CON1", fa ("GT",    [| [|"R102"; "5"|] |]))
                o ("CON2", fa ("Delay", [| [|"30"|]; |]))
                o ("CON3", fa ("Not",   [| [|"Tag1"|]; |]))
            ] |> system.Observes.AddRange


            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [device file="cylinder.ds"] A;
    [variables] = {
        R100 = (word, 0)
        R101 = (word, 1)
        R102 = (int, 1)
    }
    [commands] = {
        CMD1 = (@Delay = 0)
        CMD2 = (@Delay = 30)
        CMD2 = (@add = 30, 50 ~ R103)
    }
    [observes] = {
        CON1 = (@GT = R102, 5)
        CON2 = (@Delay = 30)
        CON3 = (@Not = Tag1)
    }
}
"""
            logDebug $"{generated}"
            compare generated answer
