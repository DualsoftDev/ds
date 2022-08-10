namespace Engine;

using Engine.Runner;
using Engine.Graph;
using System.Diagnostics;

class Tester
{
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
        Main |> XXX;
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

        Debug.Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var opc = engine.Opc;

        var resetTag = "Reset_L_F_Main";
        if (engine.Cpu.BitsMap.ContainsKey(resetTag))
        {
            var children = engine.Cpu.RootFlows.SelectMany(f => f.ChildVertices);
            var main = children.OfType<Segment>().FirstOrDefault(c => c.Name == "Main");
            var edges = main.Edges.ToArray();

            opc.Write(resetTag, true);
            opc.Write(resetTag, false);
            opc.Write("Start_L_F_Main", true);
            //opc.Write(resetTag, true);

            opc.Write("AutoStart_L_F_Main", true);
        }
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

        Debug.Assert(!Global.IsInUnitTest);
        var engine = new EngineBuilder(text, "Cpu").Engine;
        Program.Engine = engine;
        engine.Run();

        var cds = engine.Cpu.RootFlows.SelectMany(f => f.ChildVertices);
        var m = cds.OfType<Segment>().FirstOrDefault(c => c.Name == "tester");
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
