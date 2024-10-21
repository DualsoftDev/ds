namespace T
open Dual.Common.UnitTest.FS


open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Parser.FS
open System.Collections.Generic

[<AutoOpen>]
module ModelBuildupTests1 =

    type Buildup() =
        inherit EngineTestBaseClass()

        let systemRepo = ShareableSystemRepository()
        let dsFileDir = PathManager.combineFullPathDirectory ([|@$"{__SOURCE_DIRECTORY__}"; "../../UnitTest.Model/UnitTestExample/dsFolder"|])
        let libdir = $@"{dsFileDir}lib"

        let compare = compare systemRepo libdir
        let compareExact x = compare x x

        let systemRepo = ShareableSystemRepository()

        let createSimpleSystem() =
            ModelParser.ClearDicParsingText()
            let system = DsSystem.Create4Test("My")
            let flow = Flow.Create("F", system)
            let real = Real.Create("Main", flow)
            let dev = system.LoadDeviceAs(systemRepo, "A", @$"{libdir}/cylinder/double.ds", "./cylinder/double.ds")

            let apis = system.ApiUsages
            let apiP = apis.First(fun ai -> ai.Name = "ADV")
            let apiM = apis.First(fun ai -> ai.Name = "RET")
            let callAp =
                let jobFqdn = [|"F";"A";"p"|]
                let apiItem = TaskDev((apiP),  dev.Name, system)
                apiItem.InAddress <-"%I1"
                apiItem.OutAddress<- "%Q1"
                Job(jobFqdn, system, [apiItem])
            let callAm =
                let jobFqdn = [|"F";"A";"m"|]
                let apiItem = TaskDev((apiM),  dev.Name, system)
                apiItem.InAddress <-"%I2"
                apiItem.OutAddress<- "%Q2"
                Job(jobFqdn, system, [apiItem])
            system.Jobs.AddRange([callAp; callAm])
            system, flow, real, callAp, callAm

        [<Test>]
        member __.``Model creation test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let vCallP = Call.Create( callAp, DuParentReal real)
            let vCallM = Call.Create( callAm, DuParentReal real)
            real.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, "<", vCallM)) |> ignore

            let generated = system.ToDsText(true, false)
            let answer = """
[sys] My = {
    [flow] F = {
        Main = {
            A.p < A.m;		// A.p(Call)< A.m(Call);
        }
    }
    [jobs] = {
        F.A.p = { A.ADV(%I1, %Q1); }
        F.A.m = { A.RET(%I2, %Q2); }
    }
    [device file="./cylinder/double.ds"] A; 
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

            let vCallP = Alias.Create("Main2", DuAliasTargetReal real, DuParentFlow flow, false)
            let call2 = Call.Create(callAp, DuParentFlow flow)

            flow.CreateEdge(ModelingEdgeInfo<Vertex>(vCallP, "<", call2)) |> ignore
            let generated = system.ToDsText(true, false)
            let answer = """
[sys] My = {
    [flow] F = {
        Main2 < A.p;		// Main2(Alias)< A.p(Call);
        Main; // island
        [aliases] = {
            Main = { Main2; }
        }
    }
    [jobs] = {
        F.A.p = { A.ADV(%I1, %Q1); }
        F.A.m = { A.RET(%I2, %Q2); }
    }
    [device file="./cylinder/double.ds"] A; 
}
"""
            logDebug $"{generated}"
            compare generated answer


        [<Test>]
        member __.``Model with other flow real call test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()

            let flow2 = Flow.Create("F2", system)

            let real2 = Alias.Create(real.ParentNPureNames.Combine("_"), DuAliasTargetReal real, DuParentFlow flow2, false)
            let real3 = Real.Create("R3", flow2)

            flow2.CreateEdge(ModelingEdgeInfo<Vertex>(real2, ">", real3)) |> ignore
            let generated = system.ToDsText(true, false)
            let answer = """

[sys] My = {
    [flow] F = {
        Main; // island
    }
    [flow] F2 = {
        F_Main > R3;		// F_Main(Alias)> R3(Real);
        [aliases] = {
            F.Main = { F_Main; }
        }
    }
    [jobs] = {
        F.A.p = { A.ADV(%I1, %Q1); }
        F.A.m = { A.RET(%I2, %Q2); }
    }
    [device file="./cylinder/double.ds"] A; // C:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExample/dsFolder/lib/cylinder/double.ds
}
"""
            logDebug $"{generated}"
            compare generated answer

        [<Test>]
        member __.``Model with export api test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()
            let real2 = Real.Create("Main2", flow)
            let adv = ApiItem.Create("Adv", system, real, real)
            let ret = ApiItem.Create("Ret", system, real2, real2)
            [ adv; ret; ].Iter(system.ApiItems.Add >> ignore)

            ApiResetInfo.Create(system, "Adv", ModelingEdgeType.Interlock, "Ret", false) |> ignore

            let generated = system.ToDsText(true, false)
            let answer = """
[sys] My = {
    [flow] F = {
        Main, Main2; // island
    }
    [jobs] = {
        F.A.p = { A.ADV(%I1, %Q1); }
        F.A.m = { A.RET(%I2, %Q2); }
    }
    [interfaces] = {
        Adv = { F.Main ~ F.Main }
        Ret = { F.Main2 ~ F.Main2 }
        Adv <|> Ret;
    }
    [device file="./cylinder/double.ds"] A; 
}
"""
            logDebug $"{generated}"
            compare generated answer



        [<Test>]
        member __.``Model with buttons test`` () =
            let system, flow, real, callAp, callAm = createSimpleSystem()
            let defParm = defaultValueParamIO()

            system.AddButtonDef(BtnType.DuEmergencyBTN, "STOP",defParm , Addresses( "%I1","%Q1"),Some flow)
            system.AddButtonDef(BtnType.DuDriveBTN, "START",defParm,  Addresses("%I1","%Q1"),Some flow)

            let flow2 = Flow.Create("F2", system)
            system.AddButtonDef(BtnType.DuEmergencyBTN, "STOP2",defParm,  Addresses("%I1","%Q1"),Some flow2)
            system.AddButtonDef(BtnType.DuDriveBTN, "START2",defParm,  Addresses("%I1","%Q1"),Some flow2)

            let generated = system.ToDsText(true, false)
            let answer = """
[sys] My = {
    [flow] F = {
        Main; // island
    }
    [flow] F2 = {
    }
    [jobs] = {
        F.A.p = { A.ADV(%I1, %Q1); }
        F.A.m = { A.RET(%I2, %Q2); }
    }
    [buttons] = {
        [d] = {
            START(%I1, %Q1) = { F; }
            START2(%I1, %Q1) = { F2; }
        }
        [e] = {
            STOP(%I1, %Q1) = { F; }
            STOP2(%I1, %Q1) = { F2; }
        }
    }
    [device file="./cylinder/double.ds"] A; 
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


//            let generated = system.ToDsText(true)
//            let answer = """
//[sys] My = {
//    [flow] F = {
//            Main; // island
//    }
//    [jobs] = {
//        Ap = { A1.ADV(%I1, %Q1); A2.ADV(%I1, %Q1); A3.ADV(%I1, %Q1); }
//        Am = { A.RET(%I2, %Q2); }
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
