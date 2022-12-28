namespace T


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

        let systemRepo = ShareableSystemRepository()
        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let compare = compare systemRepo libdir
        let compareExact x = compare x x

        let systemRepo = ShareableSystemRepository()

        let createSimpleSystem() =
            let system = DsSystem("My", "localhost")
            let flow = Flow.Create("F", system)
            let real = Real.Create("Main", flow)
            let dev = system.LoadDeviceAs(systemRepo, "A", @$"{libdir}\cylinder.ds", "cylinder.ds")

            let apis = system.ApiUsages
            let apiP = apis.First(fun ai -> ai.Name = "+")
            let apiM = apis.First(fun ai -> ai.Name = "-")
            let callAp =
                let apiItem = JobDef(apiP, "%Q1", "%I1", "", "", dev.Name)
                Job("Ap", [apiItem])
            let callAm =
                let apiItem = JobDef(apiM, "%Q2", "%I2", "", "", dev.Name)
                Job("Am", [apiItem])
            system.Jobs.AddRange([callAp; callAm])
            system, flow, real, callAp, callAm

        [<Test>]
        member __.``Model creation test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = Call.Create( callAp, DuParentReal real)
            let vCallM = Call.Create( callAm, DuParentReal real)
            real.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, ">", vCallM)) |> ignore

            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
        Main = {
            Ap > Am;
        }
    }
    [jobs] = {
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

            let vCallP = Call.Create( callAp, DuParentReal real)
            let vCallM = Call.Create( callAm, DuParentReal real)
            ( fun () ->
                // real 의 child 간 edge 를 flow 에서 생성하려 함.. should fail
                flow.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, ">", vCallM)) |> ignore
            ) |> ShouldFailWithSubstringT "not child of"

        [<Test>]
        member __.``Model with alias test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = Alias.Create("Main2", DuAliasTargetReal real, DuParentFlow flow)
            let call2 = Call.Create(callAp, DuParentFlow flow)

            flow.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, ">", call2)) |> ignore
            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
        Main2 > Ap;		// Main2(Alias)> Ap(Call);
        Main; // island
        [aliases] = {
            Main = { Main2; }
        }
    }
    [jobs] = {
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

            let real2 = RealOtherFlow.Create(real, DuParentFlow flow2)
            let real3 = Real.Create("R3", flow2)

            flow2.CreateEdge(ModelingEdgeInfo<Vertex>(real2, ">", real3)) |> ignore
            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
    }
    [flow] F2 = {
        F.Main > R3;		// F.Main(RealOtherFlow)> R3(Real);
    }
    [jobs] = {
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
            let adv = ApiItem.Create("Adv", system, [real], [real])
            let ret = ApiItem.Create("Ret", system, [real2], [real2])
            [ adv; ret; ].Iter(system.ApiItems.Add >> ignore)

            ApiResetInfo.Create(system, "Adv", ModelingEdgeType.Interlock, "Ret") |> ignore

            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
            Main2; // island
    }
    [jobs] = {
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

            system.AddButton(BtnType.DuEmergencyBTN, "STOP", "%I1","%Q1",flow)
            system.AddButton(BtnType.DuRunBTN, "START", "%I1","%Q1",flow)

            let flow2 = Flow.Create("F2", system)
            system.AddButton(BtnType.DuEmergencyBTN, "STOP2", "%I1","%Q1",flow2)
            system.AddButton(BtnType.DuRunBTN, "START2", "%I1","%Q1",flow2)

            let generated = system.ToDsText()
            let answer = """
[sys ip = localhost] My = {
    [flow] F = {
            Main; // island
    }
    [flow] F2 = {
    }
    [jobs] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [emg] = {
        STOP = { F; }
        STOP2 = { F2; }
    }
    [run] = {
        START = { F; }
        START2 = { F2; }
    }
    [device file="cylinder.ds"] A; // D:\dsA\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds
}
"""
            logDebug $"{generated}"
            compare generated answer




//사용 안함
//        [<Test>]
//        member __.``Model with code element test`` () =
//            let system, flow, real, callAp, callAm = createSimpleSystem()

//            let v = CodeElements.VariableData
//            let c = CodeElements.Command
//            let o = CodeElements.Observe
//            [
//                v("R100", "word", "0")
//                v("R101", "word", "1")
//                v("R102", "int", "1")
//            ] |> system.Variables.AddRange

//            (*
//                [commands] = {
//                    CMD1 = (@Delay = 0)
//                    CMD2 = (@Delay = 30)
//                    CMD3 = (@add = 30, 50 ~ R103)  //30+R101 = R103
//                }
//            *)
//            let fa = FunctionApplication
//            [
//                c("CMD1", fa("Delay", [| [|"0"|] |]))
//                c("CMD2", fa("Delay", [| [|"30"|] |]))
//                c("CMD2", fa("add",   [| [|"30"; "50"|]; [|"R103"|] |]))
//            ] |> system.Commands.AddRange

//            (*
//                [observes] = {
//                    CON1 = (@GT = R102, 5)
//                    CON2 = (@Delay = 30)
//                    CON3 = (@Not = Tag1)
//                }
//            *)
//            [
//                o ("CON1", fa ("GT",    [| [|"R102"; "5"|] |]))
//                o ("CON2", fa ("Delay", [| [|"30"|]; |]))
//                o ("CON3", fa ("Not",   [| [|"Tag1"|]; |]))
//            ] |> system.Observes.AddRange


//            let generated = system.ToDsText()
//            let answer = """
//[sys ip = localhost] My = {
//    [flow] F = {
//            Main; // island
//    }
//    [jobs] = {
//        Ap = { A."+"(%Q1, %I1); }
//        Am = { A."-"(%Q2, %I2); }
//    }
//    [device file="cylinder.ds"] A;
//    [variables] = {
//        R100 = (word, 0)
//        R101 = (word, 1)
//        R102 = (int, 1)
//    }
//    [commands] = {
//        CMD1 = (@Delay = 0)
//        CMD2 = (@Delay = 30)
//        CMD2 = (@add = 30, 50 ~ R103)
//    }
//    [observes] = {
//        CON1 = (@GT = R102, 5)
//        CON2 = (@Delay = 30)
//        CON3 = (@Not = Tag1)
//    }
//}
//"""
//            logDebug $"{generated}"
//            compare generated answer
