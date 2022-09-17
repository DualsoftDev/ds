using Engine.Base;
using Engine.Graph;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Engine;

[Flags]
public enum EnumTest
{
    None = 0,
    A,
    B,
    C,
};
public class Tester
{
    public static string CreateCylinder(string name) => $"[sys] {name} =\r\n" + @"{
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <||> Vm;
    }
}";

    public static void DoSampleTest()
    {
        var text = @"
//[sys] L = {
//    [task] T = {
//        Cp = {P.F.Vp ~ P.F.Sp}
//        Cm = {P.F.Vm ~ P.F.Sm}
//    }
//    [flow] F = {
//        Main = { T.Cp > T.Cm; }
//    }
//}

[sys] L = {
    [alias] = {
        P.F.Vp = { Vp1; Vp2; Vp3; }
        P.F.Vm = { Vm1; Vm2; Vm3; }
        L.T.Cp = {Cp1; Cp2; Cp3;}
        L.T.Cm = {Cm1; Cm2; Cm3;}
    }
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
        //Cm1 = {P.F.Vm ~ P.F.Sm, P.F.Sm}
        //Cm2 = {P.F.Vm ~ P.F.Sm}
        //Cm3 = {P.F.Vm ~ P.F.Sm}
        //Cp2 = {P.F.Vp ~ P.F.Sp}
    }
    [flow] F = {
        Main = { T.Cp > T.Cm; }
        //Main = { T.Cp |> T.Cm, T.Cm1 > T.Cm2; T.Cm3 > T.Cm2; Cp2 > Cm2}
        //Main = { T.Cp2 |> Cp2; }
        //Main = { Cp2 |> Cm2; }
        //Main > Weak;
        //Cp1 > Cm1;
        //Weak >> Strong;
        //Main |> XXX;
        //parenting = {A > B > C; C |> B; }
        //T.C1 <||> T.C2;
        //A, B > C > D, E;
        //T.Cm > T.Cp;
        //T.Cm |> T.Cp;
    }
}
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Pp |> Sm;
        Pm |> Sp;
        Vp <||> Vm;
    }
}
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] OtherCpu = {
        P.F;
    }
}
[layouts] = {
       L.T.Cp = (30, 50)            // xy
       L.T.Cm = (60, 50, 20, 20)    // xywh
}
";

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var data = engine.Data;

        //var resetTag = "Reset_L_F_Main";
        //if (engine.Cpu.BitsMap.ContainsKey(resetTag))
        //{
        //    var children = engine.Cpu.RootFlows.SelectMany(f => f.ChildVertices);
        //    var main = children.OfType<Segment>().FirstOrDefault(c => c.Name == "Main");
        //    var edges = main.Edges.ToArray();

        //    data.Write(resetTag, true);
        //    data.Write(resetTag, false);
        //    data.Write("ManualStart_L_F_Main", true);
        //    //data.Write(resetTag, true);

        //    data.Write("AutoStart_L_F_Main", true);
        //}

        var startTag = "Start_L_F_Main";
        if (engine.Model.Cpus.SelectMany(cpu => cpu.BitsMap.Keys).Contains(startTag))
        {
            data.Write(startTag, true);
            data.Write("Auto_L_F", true);
            //data.Write(resetTag, true);

            //data.Write("AutoStart_L_F_Main", true);
            //data.Write("ManualStart_A_F_Pp", true);
        }

        engine.Wait();
    }
    public static void DoSampleTestAddressesAndLayouts()
    {
        var text = @"
[sys] LS_Demo = {
	[flow] page01 = { 	//Ex1_Diamond
		Work1 = {
			IO.Am <||> IO.Ap;
			IO.Am, IO.Bp > IO.Bm;
			IO.Ap > IO.Am;
			IO.Ap > IO.Bp;
			IO.Bm <||> IO.Bp;
		}
	}
	[flow] page02 = { 	//Ex2_RGB
		B ><| R;           // 인과 + 후행 reset   B ><| R, R |> B
		G => B;
		R => G;
	}

	[flow] page03 = { 	//Ex1_Diamond
		Am;
		Ap;
		Bm;
		Bp;
	}

	[task] IO = {
		Am = {EX_Am_Ap.F.Am ~ EX_Am_Ap.F.Am}
		Ap = {EX_Am_Ap.F.Ap ~ EX_Am_Ap.F.Ap}
		Bm = {EX_Bm_Bp.F.Bm ~ EX_Bm_Bp.F.Bm}
		Bp = {EX_Bm_Bp.F.Bp ~ EX_Bm_Bp.F.Bp}
	}
} //C:\Users\kwak\Downloads\LS_Demo.pptx

//LS_Demo ExRealSegments system auto generation
//LS_Demo CallSegments system auto generation
[sys] EX_Am_Ap = {
	[flow] F = { Am  <||>  Ap; }
}

[sys] EX_Bm_Bp = {
	[flow] F = { Bm  <||>  Bp; }
}
[layouts] = {
	LS_Demo.IO.Ap = (1237,437,144,144)
	LS_Demo.IO.Bp = (1244,653,144,144)
	LS_Demo.IO.Am = (1381,299,144,144)
	LS_Demo.IO.Bm = (1474,758,144,144)
}
[addresses] = {
	EX_Am_Ap.F.Am = (%Q123.23, , %I12.1);
	EX_Am_Ap.F.Ap = (%Q123.24, , %I12.2);
	EX_Bm_Bp.F.Bm = (%Q123.25, , %I12.3);
	EX_Bm_Bp.F.Bp = (%Q123.26, , %I12.4);
}

[cpus] AllCpus = {
	[cpu] Cpu = {
		LS_Demo.page01;
		LS_Demo.page02;
		LS_Demo.page03;
	}
	[cpu] ACpu = {
		EX_Am_Ap.F;
	}
	[cpu] BCpu = {
		EX_Bm_Bp.F;
	}
}

";
        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var Data = engine.Data;

        engine.Wait();
    }

    public static void DoSampleTestAdvanceReturn()
    {
        var text = @"
[sys] L = {
    [task] T = {
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
    }
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            T.Ap <||> T.Am;
            T.Ap > T.Am;
        }
    }
}
[addresses] = {
	//L.F.Main = (%0, %0,);
	A.F.Vp = (%QX0.1.3, ,);
	A.F.Vm = (%QX0.1.2, ,);
	A.F.Sp = (, , %IX0.0.5);
	A.F.Sm = (, , %IX0.0.4);
}
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] ACpu = {
        A.F;
    }
}
" + CreateCylinder("A")
;

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();


        var data = engine.Data;

        var startTag = "StartPlan_L_F_Main";
        var resetTag = "ResetPlan_L_F_Main";
        var counter = 0;
        Global.SegmentStatusChangedSubject.Subscribe(ssc =>
        {
            var qName = ssc.Segment.QualifiedName;
            var state = ssc.Status;
            if (qName == "L_F_Main")
            {
                if (state == DsType.Status4Temp.Finish)
                {
                    counter++;
                    Console.WriteLine($"[{counter}] After finishing Main segment : AdvanceReturn");
                    LogInfo($"-------------------------- [{counter}] After finishing Main segment : AdvanceReturn");
                    data.Write(resetTag, true);
                }
                else if (ssc.Status == DsType.Status4Temp.Ready)
                {
                    Console.Beep(10000, 200);
                    Thread.Sleep(1000);
                    data.Write(startTag, true);
                }
            }
        });

        var actuals =
            Global.TagChangeFromOpcServerSubject
                .Where(otc => otc.TagName.Contains("Actual"))
                ;

        // https://stackoverflow.com/questions/8837665/how-to-split-an-observable-stream-in-chunks-dependent-on-second-stream
        var oneCycleHistory =
            actuals.Publish(otcs =>
                otcs.Where(otc => otc.TagName == "StartActual_A_F_Vp" && otc.Value)       // a+ 출력 켜진 후부터
                    .Select(obs =>
                        otcs.TakeUntil(otc => otc.TagName == "StartActual_A_F_Vm" && !otc.Value) // a- 출력 꺼질때까지
                        .StartWith(obs)
                        .ToArray()))
                .Merge()
                ;

        oneCycleHistory.Subscribe(o =>
        {
            if (Global.IsDebugStopAndGoStressMode)
                return;

            Assert(o.Length == 8);

            Assert(o[0].TagName == "StartActual_A_F_Vp" && o[0].Value == true);   // a+
            Assert(o[1].TagName == "EndActual_A_F_Sm" && o[1].Value == false);  // !A-
            Assert(o[2].TagName == "EndActual_A_F_Sp" && o[2].Value == true);   // A+
            Assert(o[3].TagName == "StartActual_A_F_Vp" && o[3].Value == false);  // !a+

            Assert(o[4].TagName == "StartActual_A_F_Vm" && o[4].Value == true);   // a-
            Assert(o[5].TagName == "EndActual_A_F_Sp" && o[5].Value == false);  // !A+
            Assert(o[6].TagName == "EndActual_A_F_Sm" && o[6].Value == true);   // A-
            Assert(o[7].TagName == "StartActual_A_F_Vm" && o[7].Value == false);   // !a-
        });


        var hasAddress =
            engine.Model.Cpus
                .SelectMany(cpu => cpu.TagsMap.Values)
                .OfType<TagA>()
                .Any(t => !t.Address.IsNullOrEmpty())
                ;
        if (hasAddress)
        {
            InterlockChecker.CreateFromCylinder(data, new[] { "A_F" });

            // 모든 출력 끊기
            data.Write("StartActual_A_F_Vp", false);
            data.Write("StartActual_A_F_Vm", false);

            // simulating physics
            if (Global.IsControlMode)
            { }   // todo : 실물 연결
            else
            {
                //// initial condition
                //data.Write("EndActual_A_F_Sm", true);
                //data.Write("EndActual_B_F_Sm", true);
                Simulator.CreateFromCylinder(data, new[] { "A_F" });
            }


            //bool onceAp = true;
            bool onceMain = true;
            // simulating physics
            Global.BitChangedSubject
                .Subscribe(async bc =>
                {
                    var n = bc.Bit.GetName();
                    var val = bc.Bit.Value;
                    var monitors = new[] {
                        "StartPlan_A_F_Vp", "StartPlan_A_F_Vm",
                        "EndPlan_A_F_Sp", "EndPlan_A_F_Sm" };
                    if (monitors.Contains(n))
                    {
                        LogInfo($"Simualting Plan for Sensor {n} value={val}");
                        if (n == "StartPlan_A_F_Vp")
                            data.Write("StartActual_A_F_Vp", val);
                        else if (n == "StartPlan_A_F_Vm")
                            data.Write("StartActual_A_F_Vm", val);

                        //if (n == "EndPlan_A_F_Sp")
                        //    data.Write("EndActual_A_F_Sp", val);
                        //else if (n == "EndPlan_A_F_Sm")
                        //    data.Write("EndActual_A_F_Sm", val);
                    }

                    //if (Global.IsDebugStopAndGoStressMode && bc.Bit.Cpu.Name == "ACpu")
                    //{
                    //    if (onceAp && n == "StartActual_A_F_Vp" && val)
                    //    {
                    //        onceAp = false;
                    //        await Task.Delay(20);
                    //        LogInfo($"출력 끊기 simulation: {n}");
                    //        // opc 보다 먼저 변경을 알림...
                    //        Global.DebugNotifyingSubject.OnNext(("StartActual_A_F_Vp", false));
                    //        data.Write("StartActual_A_F_Vp", false);

                    //        LogDebug($"출력 되살리기 simulation: {n}");
                    //        await Task.Delay(20);
                    //        data.Write("StartActual_A_F_Vp", true);
                    //    }

                    //    if (!onceAp && n == "EndActual_A_F_Sm" && val)
                    //        onceAp = true;
                    //}

                    if (Global.IsDebugStopAndGoStressMode && n == startTag && val && onceMain)
                    {
                        onceMain = false;
                        await Task.Delay(50);
                        LogInfo($"출력 끊기 simulation: {n}");
                        data.Write(startTag, false);
                        await Task.Delay(50);
                        LogDebug($"출력 되살리기 simulation: {n}");
                        data.Write(startTag, true);
                    }
                });


        }

        Assert(engine.Model.Cpus.SelectMany(cpu => cpu.BitsMap.Keys).Contains(startTag));
        data.Write("Auto_L_F", true);
        data.Write(startTag, true);

        engine.Wait();
    }

    public static void DoSampleTestHatOnHat()
    {
        var text = @"
[sys] L = {
    [task] T = {
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
    }
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            T.Ap <||> T.Am;
            T.Bp <||> T.Bm;
            T.Ap > T.Bp > T.Bm > T.Am;
        }
    }
}
[addresses] = {
	L.F.Main = (%0, %1,);
	A.F.Vp = (%Q123.23, ,);
	A.F.Vm = (%Q123.24, ,);
	B.F.Vp = (%Q123.25, ,);
	B.F.Vm = (%Q123.24, ,);
	A.F.Sp = (, , %I12.2);
	A.F.Sm = (, , %I12.3);
	B.F.Sp = (, , %I12.3);
	B.F.Sm = (, , %I12.3);
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
" + CreateCylinder("A") + "\r\n" + CreateCylinder("B");

        //Log4NetHelper.ChangeLogLevel(log4net.Core.Level.Error);

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var data = engine.Data;

        var startTag = "StartPlan_L_F_Main";
        var resetTag = "ResetPlan_L_F_Main";
        var counter = 0;
        Global.SegmentStatusChangedSubject.Subscribe(ssc =>
        {
            var qName = ssc.Segment.QualifiedName;
            var state = ssc.Status;
            if (qName == "VPS_L_F_Main")
            {
                if (state == DsType.Status4Temp.Finish)
                {
                    LogDebug($"Resetting externally {resetTag}");
                    data.Write(resetTag, true);
                }
            }


            if (qName == "L_F_Main")
            {
                if (state == DsType.Status4Temp.Finish)
                {
                    counter++;
                    //if (counter++ % 100 == 0)
                    {
                        Console.WriteLine($"[{counter}] After finishing Main segment : HatOnHat");
                        LogInfo($"-------------------------- [Progress: {counter}] After finishing Main segment : HatOnHat");
                        //engine.Model.Print();
                    }
                    //data.Write(resetTag, true);
                }
                else if (ssc.Status == DsType.Status4Temp.Ready)
                {
                    Console.Beep(10000, 200);
                    Thread.Sleep(1000);
                    data.Write(startTag, true);
                }
            }
        });

        var actuals =
            Global.TagChangeFromOpcServerSubject
                .Where(otc => otc.TagName.Contains("Actual"))
                ;

        // https://stackoverflow.com/questions/8837665/how-to-split-an-observable-stream-in-chunks-dependent-on-second-stream
        var oneCycleHistory =
            actuals.Publish(otcs =>
                otcs.Where(otc => otc.TagName == "StartActual_A_F_Vp" && otc.Value)       // a+ 출력 켜진 후부터
                    .Select(obs =>
                        otcs.TakeUntil(otc => otc.TagName == "StartActual_A_F_Vm" && !otc.Value) // a- 출력 꺼질때까지
                        .StartWith(obs)
                        .ToArray()))
                .Merge()
                ;
        oneCycleHistory.Subscribe(o =>
        {
            if (Global.IsDebugStopAndGoStressMode)
                return;

            Assert(o[0].TagName == "StartActual_A_F_Vp" && o[0].Value == true);   // a+
            Assert(o[1].TagName == "EndActual_A_F_Sm" && o[1].Value == false);  // !A-
            Assert(o[2].TagName == "EndActual_A_F_Sp" && o[2].Value == true);   // A+
            Assert(o[3].TagName == "StartActual_A_F_Vp" && o[3].Value == false);  // !a+


            Assert(o[4].TagName == "StartActual_B_F_Vp" && o[4].Value == true);   // b+
            Assert(o[5].TagName == "EndActual_B_F_Sm" && o[5].Value == false);  // !B-
            Assert(o[6].TagName == "EndActual_B_F_Sp" && o[6].Value == true);   // B+
            Assert(o[7].TagName == "StartActual_B_F_Vp" && o[7].Value == false);  // !b+


            Assert(o[8].TagName == "StartActual_B_F_Vm" && o[8].Value == true);   // b-
            Assert(o[9].TagName == "EndActual_B_F_Sp" && o[9].Value == false);  // !B+
            Assert(o[10].TagName == "EndActual_B_F_Sm" && o[10].Value == true);   // B-
            Assert(o[11].TagName == "StartActual_B_F_Vm" && o[11].Value == false);  // !b-

            Assert(o[12].TagName == "StartActual_A_F_Vm" && o[12].Value == true);   // a-
            Assert(o[13].TagName == "EndActual_A_F_Sp" && o[13].Value == false);  // !A+
            Assert(o[14].TagName == "EndActual_A_F_Sm" && o[14].Value == true);   // A-
            Assert(o[15].TagName == "StartActual_A_F_Vm" && o[15].Value == false);  // !a-
        });

        //actuals
        //    .Subscribe(otc =>
        //    {
        //        Trace.WriteLine($"{otc.TagName}={otc.Value}");
        //    });

        var hasAddress =
            engine.Model.Cpus
                .SelectMany(cpu => cpu.TagsMap.Values)
                .OfType<TagA>()
                .Any(t => !t.Address.IsNullOrEmpty())
                ;
        if (hasAddress)
        {
            // initial condition
            data.Write("EndActual_A_F_Sm", true);
            data.Write("EndActual_B_F_Sm", true);

            InterlockChecker.CreateFromCylinder(data, new[] { "A_F", "B_F" });
            // simulating physics
            if (Global.IsControlMode)
            { }   // todo : 실물 연결
            else
                Simulator.CreateFromCylinder(data, new[] { "A_F", "B_F" });

            Global.BitChangedSubject
                .Subscribe(bc =>
                {
                    var n = bc.Bit.GetName();
                    var val = bc.Bit.Value;
                    var monitors = new[] {
                        "StartPlan_A_F_Vp", "StartPlan_B_F_Vp", "StartPlan_A_F_Vm", "StartPlan_B_F_Vm",
                        "EndPlan_A_F_Sp", "EndPlan_A_F_Sm", "EndPlan_B_F_Sp", "EndPlan_B_F_Sm" };
                    if (monitors.Contains(n))
                    {
                        LogDebug($"Plan for TAG {n} value={val}");

                        if (val)
                        {
                            if (n == "StartPlan_A_F_Vp")
                                data.Write("StartActual_A_F_Vp", val);
                            else if (n == "StartPlan_B_F_Vp")
                                data.Write("StartActual_B_F_Vp", val);
                            else if (n == "StartPlan_A_F_Vm")
                                data.Write("StartActual_A_F_Vm", val);
                            else if (n == "StartPlan_B_F_Vm")
                                data.Write("StartActual_B_F_Vm", val);
                        }

                        //else if (n == "EndPlan_A_F_Sp")
                        //    data.Write("EndActual_A_F_Sp", val);
                        //else if (n == "EndPlan_A_F_Sm")
                        //    data.Write("EndActual_A_F_Sm", val);
                        //else if (n == "EndPlan_B_F_Sp")
                        //    data.Write("EndActual_B_F_Sp", val);
                        //else if (n == "EndPlan_B_F_Sm")
                        //    data.Write("EndActual_B_F_Sm", val);
                    }
                });
        }


        Assert(engine.Model.Cpus.SelectMany(cpu => cpu.BitsMap.Keys).Contains(startTag));
        data.Write(startTag, true);
        data.Write("Auto_L_F", true);

        engine.Wait();
    }

    public static string GetTextDiamondNormal()
    {
        var text = @"
[sys] L = {
    [task] T = {
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
    }
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            T.Ap <||> T.Am;
            T.Bp <||> T.Bm;
            T.Ap > T.Am, T.Bp > T.Bm;
        }
    }
}
[addresses] = {
	L.F.Main = (%0, %0,);
	A.F.Vp = (%QX0.1.3, ,);
	A.F.Vm = (%QX0.1.2, ,);
	B.F.Vp = (%QX0.1.5, ,);
	B.F.Vm = (%QX0.1.4, ,);
	A.F.Sp = (, , %IX0.0.5);
	A.F.Sm = (, , %IX0.0.4);
	B.F.Sp = (, , %IX0.0.7);
	B.F.Sm = (, , %IX0.0.6);
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
" + CreateCylinder("A") + "\r\n" + CreateCylinder("B");
        return text;
    }

    /// <summary>Diamond with flow task</summary>
    public static string GetTextDiamond()
    {
        var text = @"
[sys] L = {
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            Ap <||> Am;
            Bp <||> Bm;
            Ap > Am, Bp > Bm;
        }
        [task] = {
            Ap = {A.F.Vp ~ A.F.Sp}
            Am = {A.F.Vm ~ A.F.Sm}
            Bp = {B.F.Vp ~ B.F.Sp}
            Bm = {B.F.Vm ~ B.F.Sm}
        }
    }
}
[addresses] = {
	L.F.Main = (%0, %0,);
	A.F.Vp = (%QX0.1.3, ,);
	A.F.Vm = (%QX0.1.2, ,);
	B.F.Vp = (%QX0.1.5, ,);
	B.F.Vm = (%QX0.1.4, ,);
	A.F.Sp = (, , %IX0.0.5);
	A.F.Sm = (, , %IX0.0.4);
	B.F.Sp = (, , %IX0.0.7);
	B.F.Sm = (, , %IX0.0.6);
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
" + CreateCylinder("A") + "\r\n" + CreateCylinder("B");
        return text;
    }
    public static void DoSampleTestDiamond()
    {

        //Log4NetHelper.ChangeLogLevel(log4net.Core.Level.Error);

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(GetTextDiamond(), "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();


        {
            var cds = engine.Cpu.RootFlows.SelectMany(f => f.ChildVertices);
            var m = cds.OfType<SegmentBase>().FirstOrDefault(c => c.Name == "Main");

            // test origin
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var t = new GraphProgressSupportUtil.ProgressInfo(m.GraphInfo);
            stopwatch.Stop();
            Console.WriteLine("time : " + stopwatch.ElapsedMilliseconds + "ms");
            t.PrintIndexedChildren();
            t.PrintPreCaculatedTargets();
            t.PrintOrigin();
        }

        var data = engine.Data;

        var startTag = "StartPlan_L_F_Main";
        var resetTag = "ResetPlan_L_F_Main";
        var counter = 0;
        Global.SegmentStatusChangedSubject.Subscribe(ssc =>
        {
            var qName = ssc.Segment.QualifiedName;
            var state = ssc.Status;

            if (qName == "L_F_Main")
            {
                if (state == DsType.Status4Temp.Finish)
                {
                    counter++;
                    //if (counter++ % 100 == 0)
                    {
                        Console.WriteLine($"[{counter}] After finishing Main segment : Diamond");
                        LogInfo($"-------------------------- [Progress: {counter}] After finishing Main segment : Diamond");
                        //engine.Model.Print();
                    }
                    data.Write(resetTag, true);
                }
                else if (ssc.Status == DsType.Status4Temp.Ready)
                {
                    Console.Beep(10000, 200);
                    Thread.Sleep(1000);
                    data.Write(startTag, true);
                }
            }
        });

        var actuals =
            Global.TagChangeFromOpcServerSubject
                .Where(otc => otc.TagName.Contains("Actual"))
                ;

        // https://stackoverflow.com/questions/8837665/how-to-split-an-observable-stream-in-chunks-dependent-on-second-stream
        var oneCycleHistory =
            actuals.Publish(otcs =>
                otcs.Where(otc => otc.TagName == "StartActual_A_F_Vp" && otc.Value)       // a+ 출력 켜진 후부터
                    .Select(obs =>
                        otcs.TakeUntil(otc => otc.TagName == "StartActual_B_F_Vm" && !otc.Value) // b- 출력 꺼질때까지
                        .StartWith(obs)
                        .ToArray()))
                .Merge()
                ;
        oneCycleHistory.Subscribe(o =>
        {
            if (Global.IsDebugStopAndGoStressMode)
                return;

            Assert(o.Length == 16);

            Assert(o[0].TagName == "StartActual_A_F_Vp" && o[0].Value == true);   // a+
            Assert(o[1].TagName == "EndActual_A_F_Sm" && o[1].Value == false);  // !A-
            Assert(o[2].TagName == "EndActual_A_F_Sp" && o[2].Value == true);   // A+
            Assert(o[3].TagName == "StartActual_A_F_Vp" && o[3].Value == false);  // !a+

            //Assert(o[4].TagName == "StartActual_A_F_Vm" && o[4].Value == true);   // a-
            //Assert(o[5].TagName == "StartActual_B_F_Vp" && o[5].Value == true);   // b+
            var nam = o.FindIndex(otc => otc.TagName == "StartActual_A_F_Vm" && otc.Value == true);  // a-
            var nbp = o.FindIndex(otc => otc.TagName == "StartActual_B_F_Vp" && otc.Value == true);  // b+
            Assert(nam.IsOneOf(4, 5) && nbp.IsOneOf(4, 5));

            // oN, ofF
            var fAp = o.FindIndex(otc => otc.TagName == "EndActual_A_F_Sp" && otc.Value == false);  // !A+
            var fBm = o.FindIndex(otc => otc.TagName == "EndActual_B_F_Sm" && otc.Value == false);  // !B-
            var nBp = o.FindIndex(otc => otc.TagName == "EndActual_B_F_Sp" && otc.Value == true);   // B+
            var fbp = o.FindIndex(otc => otc.TagName == "StartActual_B_F_Vp" && otc.Value == false);  // !b+
            var nAm = o.FindIndex(otc => otc.TagName == "EndActual_A_F_Sm" && otc.Value == true);   // A-
            var fam = o.FindIndex(otc => otc.TagName == "StartActual_A_F_Vm" && otc.Value == false);  // !a-

            Assert(fAp < nAm && nAm < fam);
            Assert(fBm < nBp && nBp < fbp);

            //Assert(o[ 6].TagName == "EndActual_A_F_Sp"     && o[ 6].Value == false);  // !A+
            //Assert(o[ 7].TagName == "EndActual_B_F_Sm"     && o[ 7].Value == false);  // !B-
            //Assert(o[ 8].TagName == "EndActual_B_F_Sp"     && o[ 8].Value == true);   // B+
            //Assert(o[ 9].TagName == "StartActual_B_F_Vp"   && o[ 9].Value == false);  // !b+
            //Assert(o[10].TagName == "EndActual_A_F_Sm"     && o[10].Value == true);   // A-
            //Assert(o[11].TagName == "StartActual_A_F_Vm"   && o[11].Value == false);  // !a-

            Assert(o[12].TagName == "StartActual_B_F_Vm" && o[12].Value == true);   // b-
            Assert(o[13].TagName == "EndActual_B_F_Sp" && o[13].Value == false);  // !B+
            Assert(o[14].TagName == "EndActual_B_F_Sm" && o[14].Value == true);   // B-
            Assert(o[15].TagName == "StartActual_B_F_Vm" && o[15].Value == false);   // !b-
        });

        //actuals
        //    .Subscribe(otc =>
        //    {
        //        Trace.WriteLine($"{otc.TagName}={otc.Value}");
        //    });

        var hasAddress =
            engine.Model.Cpus
                .SelectMany(cpu => cpu.TagsMap.Values)
                .OfType<TagA>()
                .Any(t => !t.Address.IsNullOrEmpty())
                ;
        if (hasAddress)
        {
            InterlockChecker.CreateFromCylinder(data, new[] { "A_F", "B_F" });

            // 모든 출력 끊기
            data.Write("StartActual_A_F_Vp", false);
            data.Write("StartActual_A_F_Vm", false);
            data.Write("StartActual_B_F_Vp", false);
            data.Write("StartActual_B_F_Vm", false);

            // simulating physics
            if (Global.IsControlMode)
            { }   // todo : 실물 연결
            else
            {
                //// initial condition
                data.Write("EndActual_A_F_Sm", true);
                data.Write("EndActual_B_F_Sm", true);
                Simulator.CreateFromCylinder(data, new[] { "A_F", "B_F" });
            }

            Global.BitChangedSubject
                .Subscribe(bc =>
                {
                    var n = bc.Bit.GetName();
                    var val = bc.Bit.Value;
                    var monitors = new[] {
                        "StartPlan_A_F_Vp", "StartPlan_B_F_Vp", "StartPlan_A_F_Vm", "StartPlan_B_F_Vm",
                        "EndPlan_A_F_Sp", "EndPlan_A_F_Sm", "EndPlan_B_F_Sp", "EndPlan_B_F_Sm" };
                    if (monitors.Contains(n))
                    {
                        LogDebug($"Plan for TAG {n} value={val}");

                        if (val)
                        {
                            if (n == "StartPlan_A_F_Vp")
                                data.Write("StartActual_A_F_Vp", val);
                            else if (n == "StartPlan_B_F_Vp")
                                data.Write("StartActual_B_F_Vp", val);
                            else if (n == "StartPlan_A_F_Vm")
                                data.Write("StartActual_A_F_Vm", val);
                            else if (n == "StartPlan_B_F_Vm")
                                data.Write("StartActual_B_F_Vm", val);
                        }

                        //else if (n == "EndPlan_A_F_Sp")
                        //    data.Write("EndActual_A_F_Sp", val);
                        //else if (n == "EndPlan_A_F_Sm")
                        //    data.Write("EndActual_A_F_Sm", val);
                        //else if (n == "EndPlan_B_F_Sp")
                        //    data.Write("EndActual_B_F_Sp", val);
                        //else if (n == "EndPlan_B_F_Sm")
                        //    data.Write("EndActual_B_F_Sm", val);
                    }
                });
        }


        Assert(engine.Model.Cpus.SelectMany(cpu => cpu.BitsMap.Keys).Contains(startTag));
        data.Write(startTag, true);
        data.Write("Auto_L_F", true);

        engine.Wait();
    }


    public static void DoSampleTestTriangle()
    {
        var text = @"
[sys] L = {
    [flow] F = {
        R > G > B > R;
        R, G |> B |> G |> R        
    }
}

[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
}
";

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var Data = engine.Data;

        var startTag = "StartPlan_L_F_B";

        var rootSegments =
            engine.Model.Systems
            .SelectMany(s => s.RootFlows)
            .SelectMany(f => f.RootSegments)
            .Distinct()
            ;
        var coutner = rootSegments.ToDictionary(s => s, _ => 0);
        bool first = true;
        var finished =
            Global.SegmentStatusChangedSubject
            .Where(ssc => ssc.Status == DsType.Status4Temp.Finish && ssc.Segment.GetType().Name == "Segment")
            ;
        var _counterSubscription = finished.Subscribe(ssc =>
            {
                coutner[ssc.Segment] = coutner[ssc.Segment] + 1;

                var avg = (int)coutner.Values.Average(s => s);
                Assert(coutner.Values.ForAll(v => Math.Abs(avg - v) <= 1));

                if (ssc.Segment.QualifiedName == "L.F.B")
                {
                    LogInfo("-------------------------- End of Main segment");
                    if (first)
                    {
                        Data.Write(startTag, false);
                        first = false;
                    }
                }
            });

        var _checkOrderSubscription = finished.Buffer(3).Subscribe(sscs =>
        {
            var order = sscs.Select(ssc => ssc.Segment.Name).ToArray();
            Assert(order.SequenceEqual(new[] { "B", "R", "G", }));
        });

        Assert(engine.Model.Cpus.SelectMany(cpu => cpu.BitsMap.Keys).Contains(startTag));
        Data.Write(startTag, true);
        Data.Write("Auto_L_F", true);

        engine.Wait();
    }


    public static void DoSampleTestVps()
    {

        var text = @"
[sys] L = {
    [task] T = {
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
    }
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            T.Ap <||> T.Am;
            T.Ap > T.Am;
        }
    }
}
[sys] A = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Sp |> Pp |> Sm;
        Sm |> Pm |> Sp;
        Vp <||> Vm;
    }
}
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] ACpu = {
        A.F;
    }
}
";

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var Data = engine.Data;

        Global.BitChangedSubject
            .Where(bc => bc.Bit.GetName() == "epexL_F_Main_default" && bc.Bit.Value)
            .Subscribe(bc =>
            {
                //data.Write("AutoStart_L_F", false);
                Data.Write("AutoReset_L_F", true);
            });

        Data.Write("AutoStart_L_F", true);
        //data.Write("ManualStart_L_F_Main", true);
        //data.Write("ManualStart_A_F_Pp", true);

        engine.Wait();
    }

    public static void DoSampleTest2()
    {
        var text = @"
[sys] L = {
    [alias] = {
        P.F.Vp1 = {Vp11; Vp12;}
        P.F.Vp2 = {Vp21; Vp22;}
        P.F.Vm1 = {Vm11; Vm12;}
        P.F.Vm2 = {Vm21; Vm22;}
        W.F.work = {W1; W2;}
        L.T.Wrk = {WW1; WW2;}
        W.F.aa = {A1; A2;}
        W.F.bb = {B1; B2;}
        L.T.Cp1 = {CP11; CP12; CP13; CP14; CP15; CP16; CP17; CP18;}
        L.T.Cm1 = {CM11; CM12; CM13; CM14;}
        L.T.CC = {C1; C2;}
        L.T.DD = {D1; D2;}
        L.T.EE = {E1; E2;}
    }
    [task] T = {
        Cp1 = {P.F.Vp1 ~ P.F.Sp1}
        Cm1 = {P.F.Vm1 ~ P.F.Sm1}
        Cp2 = {P.F.Vp2 ~ P.F.Sp2}
        Cm2 = {P.F.Vm2 ~ P.F.Sm2}
        Wrk = {W.F.work ~ W.F.work}
        CC = {W.F.cc ~ W.F.cc}
        DD = {W.F.dd ~ W.F.dd}
        EE = {W.F.ee ~ W.F.ee}
        FF = {W.F.ff ~ W.F.ff}
        GG = {W.F.gg ~ W.F.gg}
        HH = {W.F.hh ~ W.F.hh}
        II = {W.F.ii ~ W.F.ii}
    }
    [flow] F = {
        ////TC1
        //tester = {
        //    T.CC > T.DD > T.EE > T.FF;
        //}

        ////TC1.1
        //tester = {
        //    T.CC > T.DD > T.EE > T.FF;
        //    T.CC <| T.DD <| T.EE <| T.FF;
        //}

        ////TC2
        //tester = {
        //    Vp11 > Vp21 > Vm21 > Vm11;
        //    Vp11 |> Vm12;
        //    Vp21 |> Vm22;
        //    Vm21 |> Vp22;
        //    Vm11 |> Vp12;
        //}

        ////TC3
        //tester = {
        //    T.Cm1 < T.Cm2 < T.Cp2 < T.Cp1;
        //    T.Cp1 <||> T.Cm1;
        //    T.Cp2 <||> T.Cm2;
        //}

        ////TC3.1
        //tester = {
        //    T.Cp1 > T.Cp2 > T.Cm2 > T.Cm1;
        //    T.Cp1 |> T.Cm2 |> T.Cp2 |> T.Cm1;
        //}

        ////TC4
        //tester = {
        //    T.Cp1 > T.Cp2, T.Cm1 > T.Cm2;
        //    T.Cp1 <||> T.Cm1;
        //    T.Cp2 <||> T.Cm2;
        //}

        ////TC5
        //tester = {
        //    B1 <| A1 > T.Wrk;
        //    A2 <| T.Wrk > B2;
        //}

        ////TC5.1
        //tester = {
        //    B1 <| A1 > T.Wrk;
        //    T.Wrk > B2 |> A2;
        //}

        ////TC5.2
        //tester = {
        //    CM11 <| CP11 > CM12 |> CP12;
        //}

        ////TC5.3
        //tester = {
        //    D1 <| C2 > D2;
        //    E1 <| D2 > E2;
        //    C1 <| E2;
        //}

        ////TC5.4
        //tester = {
        //    CM11 <| CP11 > CM12;
        //    CP12 <| CM12 > CP13;
        //    CM13 <| CP13 > CM14;
        //    CP14 <| CM14;
        //}

        ////TC6
        //tester = {
        //    T.Cp1 > T.Cm1 > T.Cm2 > W1;
        //    T.Cp1 > T.Cp2 > W1;
        //    T.Cp1 <||> T.Cm1;
        //    T.Cp2 <||> T.Cm2;
        //}

        ////TC7
        //tester = {
        //    CP12 <| CP11 > CP13;
        //    CP14 <| CP13 > CP15;
        //    CP16 <| CP15 > CP17;
        //    CP18 <| CP15;
        //}

        //TC8
        tester = {
            T.FF > T.HH;
            T.GG, T.HH > T.II <||> T.CC;
            T.CC > T.DD > T.EE > T.FF, T.GG;
        }

        tester |> tester;
    }
}

[sys] P = {
    [flow] F = {
        Vp1 > Pp1 > Sp1;
        Vm1 > Pm1 > Sm1;
        Pp1 |> Sm1;
        Pm1 |> Sp1;
        Vp1 <||> Vm1;
        Pp1 <||> Pm1;

        Vp2 > Pp2 > Sp2;
        Vm2 > Pm2 > Sm2;
        Pp2 |> Sm2;
        Pm2 |> Sp2;
        Vp2 <||> Vm2;
        Pp2 <||> Pm2;
    }
}
[sys] W = {
    [flow] F = {
        work |> work;
        aa; bb;
        //cc |> ee |> ff |> dd;
        cc; dd; ee; ff; gg; hh; ii;
    }
}
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] OtherCpu = {
        P.F;
        W.F;
    }
}
";

        Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var cds = engine.Cpu.RootFlows.SelectMany(f => f.ChildVertices);
        var m = cds.OfType<SegmentBase>().FirstOrDefault(c => c.Name == "tester");
        Console.WriteLine(m.Name);
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var t = new GraphProgressSupportUtil.ProgressInfo(m.GraphInfo);
        stopwatch.Stop();
        Console.WriteLine("time : " + stopwatch.ElapsedMilliseconds + "ms");
        t.PrintIndexedChildren();
        t.PrintPreCaculatedTargets();
        t.PrintOrigin();
    }
}
