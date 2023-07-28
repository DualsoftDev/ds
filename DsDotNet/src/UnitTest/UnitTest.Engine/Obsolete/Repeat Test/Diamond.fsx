namespace T.RepeatTest


open Engine
open Engine.Core
open System.Linq
open Dual.Common.Core.FS
open Xunit.Abstractions
open T
open Engine.Common
open System.Diagnostics
open Engine.Base
open NUnit.Framework

type Diamond(output1:ITestOutputHelper) =
    inherit EngineTestBaseClass()
    
    let createCylinder = Tester.CreateCylinder
    let address = """
[addresses] = {
	//L.F.Main = (%0, %0,);
	A.F.Vp = (%Q123.23, ,);
	A.F.Vm = (%Q123.24, ,);
	B.F.Vp = (%Q123.25, ,);
	B.F.Vm = (%Q123.24, ,);
	A.F.Sp = (, , %I12.2);
	A.F.Sm = (, , %I12.3);
	B.F.Sp = (, , %I12.3);
	B.F.Sm = (, , %I12.3);
}
"""
    let text = """
[sys] L = {
    [task] T = {
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
    }
    [flow] F = {
        Main = {
            // 정보로서의 CallDev 상호 리셋
            T.Ap <||> T.Am;
            T.Bp <||> T.Bm;
            T.Ap > T.Am, T.Bp > T.Bm;
        }
    }
}

[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] ACpu = {
        A.F;
    }
    [cpu] BCpu = {
        B.F;
    }
}
"""
    let text = text + address + createCylinder("A") + "\r\n" + createCylinder("B")

    let simulate(engine:Runner.EngineModule.Engine) =
        let hasAddress =
            engine.Model.Cpus
                .selectMany(fun cpu -> cpu.TagsMap.Values)
                .OfType<TagA>()
                .Any(fun t -> not <| t.Address.IsNullOrEmpty())
                
        let opc = engine.Data
        if (hasAddress) then
            // initial condition
            opc.Write("EndActual_A_F_Sm", true)
            opc.Write("EndActual_B_F_Sm", true)

            // simulating physics
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let n = bc.Bit.GetName()
                    let value = bc.Bit.Value
                    let monitors = [
                        "StartPlan_A_F_Vp"; "StartPlan_B_F_Vp"; "StartPlan_A_F_Vm"; "StartPlan_B_F_Vm";
                        "EndPlan_A_F_Sp"; "EndPlan_A_F_Sm"; "EndPlan_B_F_Sp"; "EndPlan_B_F_Sm" ]
                    if monitors.Contains(n) then
                        logDebug $"Plan for TAG {n} value={value}"
                        match n with
                        | "StartPlan_A_F_Vp" -> opc.Write("StartActual_A_F_Vp", value)
                        | "StartPlan_B_F_Vp" -> opc.Write("StartActual_B_F_Vp", value)
                        | "StartPlan_A_F_Vm" -> opc.Write("StartActual_A_F_Vm", value)
                        | "StartPlan_B_F_Vm" -> opc.Write("StartActual_B_F_Vm", value)

                        | "EndPlan_A_F_Sp" -> opc.Write("EndActual_A_F_Sp", value)
                        | "EndPlan_A_F_Sm" -> opc.Write("EndActual_A_F_Sm", value)
                        | "EndPlan_B_F_Sp" -> opc.Write("EndActual_B_F_Sp", value)
                        | "EndPlan_B_F_Sm" -> opc.Write("EndActual_B_F_Sm", value)
                        | _ ->
                            failwithlog "ERROR"
                    ) |> ignore



    [<Test>]
    member __.``XDiamond Test`` () =
        logInfo "============== Diamond Test"
        Log4NetHelper.ChangeLogLevel(log4net.Core.Level.Error)

        let engine = EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine
        Program.Engine <- engine
        engine.Run() |> ignore

        let opc = engine.Data

        let startTag = "StartPlan_L_F_Main"
        let resetTag = "ResetPlan_L_F_Main"
        let mutable counter = 0
        Global.SegmentStatusChangedSubject.Subscribe(fun ssc ->
            if ssc.Segment.QualifiedName = "L_F_Main" then
                if ssc.Status = DsType.Status4.Finish then
                    counter <- counter + 1
                    if counter % 100 = 0 then
                        System.Console.WriteLine($"[{counter}] After finishing Main segment")
                        Trace.WriteLine($"[{counter}] After finishing Main segment")
                    opc.Write(resetTag, true)
                elif ssc.Status = Status4.Ready then
                    opc.Write(startTag, true)
        ) |> ignore

        simulate(engine)


        assert(engine.Model.Cpus.selectMany(fun cpu -> cpu.BitsMap.Keys).Contains(startTag))
        opc.Write(startTag, true)
        opc.Write("Auto_L_F", true)

        engine.Wait()

