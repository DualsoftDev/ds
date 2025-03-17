
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.Sample;

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

    public static string DoSampleTest()
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
       T.Cp = (30, 50);            // xy
       T.Cm = (60, 50, 20, 20);    // xywh
}
";

        return text;

    }
    public static string DoSampleTestAddressesAndLayouts()
    {
        var text = @"
[sys] KIT = {
    [flow] IO_9 = {
    }
    [flow] KIT = {
        ""5OUT"" |> ""5OUT_RESET"";
        KIT_Part_REMOVE > ""5OUT PASS"" => ""0IN_1"";
        A1Work_1 => ""5OUT PASS"";
        A1Work, KIT_Part_MOVE > ""5OUT"" => ""5OUT_RESET"";
        KIT_Frt_Part_REMOVE > ""0IN"";
        A1Work => ""0IN"" => A1Work;
        ""0IN"" = {
            KIT_Cv_RR_MOVE > KIT_In_ADV > KIT_In_RET;
            KIT_Cv_RR_MOVE > KIT_Cv_RR_REMOVE;
        }
        A1Work = {
            KIT_Cv_Frt_MOVE > KIT_1st_usb_ADV, KIT_2nd_usb_ADV, KIT_3rd_usb_ADV, KIT_4th_usb_ADV > KIT_1st_usb_RET, KIT_2nd_usb_RET, KIT_3rd_usb_RET, KIT_4th_usb_RET > KIT_stp_RET > KIT_Cv_Frt_REMOVE > KIT_stp_ADV;
        }
        ""5OUT"" = {
            KIT_Cv_FrtOUT_MOVE > KIT_Cv_FrtOUT_REMOVE;
            KIT_Cv_FrtOUT_MOVE > KIT_Out_ADV > KIT_Out_RET;
        }
            ""5OUT PASS222""; 
        [aliases] = {
            ""0IN"" = { ""0IN_1""; }
            A1Work = { A1Work_1; }
        }
    }
    [flow] ""Station 101"" = {
            Work1; 
            Work2; 
            Work3; 
            Work4; 
            Work5; 
            Work6; 
    }
    [flow] ""Station 102"" = {
            Work1; 
            Work2; 
            Work3; 
            Work4; 
            Work5; 
            Work6; 
    }
    [flow] ""Station 103"" = {
            Work1; 
            Work2; 
            Work3; 
            Work4; 
            Work5; 
            Work6; 
    }
    [flow] ""Station 104"" = {
            Work1; 
            Work2; 
            Work3; 
            Work4; 
            Work5; 
            Work6; 
    }
    [flow] ""Station 105"" = {
            Work1; 
            Work2; 
            Work3; 
            Work4; 
            Work5; 
            Work6; 
    }
    [jobs] = {
        KIT_Cv_Frt_MOVE = { KIT_Cv_Frt.MOVE(%IX0.0.32, %MX5); }
        KIT_Cv_Frt_REMOVE = { KIT_Cv_Frt.REMOVE(%IX0.0.32, %MX5); }
            KIT_Cv_Frt_REMOVE.func = {
                $n ;
                $t 300;
            }
        KIT_stp_ADV = { KIT_stp1.ADV(%IX0.0.5, %QX0.1.3); KIT_stp2.ADV(%IX0.0.9, %QX0.1.7); KIT_stp3.ADV(%IX0.0.13, %QX0.1.11); KIT_stp4.ADV(%IX0.0.17, %QX0.1.15); }
        KIT_stp_RET = { KIT_stp1.RET(%IX0.0.4, %QX0.1.2); KIT_stp2.RET(%IX0.0.8, %QX0.1.6); KIT_stp3.RET(%IX0.0.12, %QX0.1.10); KIT_stp4.RET(%IX0.0.16, %QX0.1.14); }
        KIT_1st_usb_ADV = { KIT_1st_usb.ADV(%IX0.0.7, %QX0.1.5); }
        KIT_1st_usb_RET = { KIT_1st_usb.RET(%IX0.0.6, %QX0.1.4); }
        KIT_2nd_usb_ADV = { KIT_2nd_usb.ADV(%IX0.0.11, %QX0.1.9); }
        KIT_2nd_usb_RET = { KIT_2nd_usb.RET(%IX0.0.10, %QX0.1.8); }
        KIT_3rd_usb_ADV = { KIT_3rd_usb.ADV(%IX0.0.15, %QX0.1.13); }
        KIT_3rd_usb_RET = { KIT_3rd_usb.RET(%IX0.0.14, %QX0.1.12); }
        KIT_4th_usb_ADV = { KIT_4th_usb.ADV(%IX0.0.19, %QX0.1.17); }
        KIT_4th_usb_RET = { KIT_4th_usb.RET(%IX0.0.18, %QX0.1.16); }
        KIT_In_ADV = { KIT_In.ADV(%IX0.0.3, %QX0.1.1); }
        KIT_In_RET = { KIT_In.RET(%IX0.0.2, %QX0.1.0); }
        KIT_Cv_FrtOUT_MOVE = { KIT_Cv_FrtOUT.MOVE(%IX0.0.52, %MX6); }
        KIT_Cv_FrtOUT_REMOVE = { KIT_Cv_FrtOUT.REMOVE(_, %QW625.4); }
            KIT_Cv_FrtOUT_REMOVE.func = {
                $n;
            }
        KIT_Cv_RR_MOVE = { KIT_Cv_RR.MOVE(%IX0.0.0, %QX0.1.28); }
        KIT_Cv_RR_REMOVE = { KIT_Cv_RR.REMOVE(%IX0.0.0, %QW625.3); }
            KIT_Cv_RR_REMOVE.func = {
                $n;
            }
        KIT_Out_ADV = { KIT_Out.ADV(%IX0.0.23, %QX0.1.19); }
        KIT_Out_RET = { KIT_Out.RET(%IX0.0.22, %QX0.1.18); }
        KIT_Part_MOVE = { KIT_Part.MOVE(%IX0.0.53, %QW625.1); }
        KIT_Part_REMOVE = { KIT_Part.REMOVE(%IX0.0.53, %QW625.2); }
            KIT_Part_REMOVE.func = {
                $n;
            }
        KIT_Frt_Part_MOVE = { KIT_Frt_Part.MOVE(%IX0.0.1, %QW625.1); }
        KIT_Frt_Part_REMOVE = { KIT_Frt_Part.REMOVE(%IX0.0.1, %QW625.2); }
            KIT_Frt_Part_REMOVE.func = {
                $n;
            }
    }
    [device file=""lib/Conveyor/CVRemove.ds""] KIT_Cv_Frt; 
    [device file=""lib/Cylinder/Double.ds""] KIT_stp1; 
    [device file=""lib/Cylinder/Double.ds""] KIT_stp2; 
    [device file=""lib/Cylinder/Double.ds""] KIT_stp3; 
    [device file=""lib/Cylinder/Double.ds""] KIT_stp4; 
    [device file=""lib/Cylinder/Double.ds""] KIT_1st_usb; 
    [device file=""lib/Cylinder/Double.ds""] KIT_2nd_usb; 
    [device file=""lib/Cylinder/Double.ds""] KIT_3rd_usb; 
    [device file=""lib/Cylinder/Double.ds""] KIT_4th_usb; 
    [device file=""lib/Cylinder/Double.ds""] KIT_In; 
    [device file=""lib/Conveyor/CV.ds""] KIT_Cv_FrtOUT; 
    [device file=""lib/Conveyor/CV.ds""] KIT_Cv_RR; 
    [device file=""lib/Cylinder/Double.ds""] KIT_Out; 
    [device file=""lib/Conveyor/CVRemove.ds""] KIT_Part;
    [device file=""lib/Conveyor/CVRemove.ds""] KIT_Frt_Part; 
    [prop] = {
        [layouts channel=""asdsadas""] = {
	        KIT_stp1.ADV = (1237,437,144,144);
	        KIT_stp2.ADV = (1244,653,144,144);
	        KIT_1st_usb = (1381,299,144,144);
	        KIT_2nd_usb = (1474,758,144,144);
        }
        [finish] = { KIT.A1Work; KIT.""5OUT""; }
        [disable] = { KIT.A1Work.KIT_Cv_Frt_MOVE; KIT.""5OUT"".KIT_Out_RET; }
    }
}
";
        return text;

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


    }

    public static string GetTextDiamondNormal()
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
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
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
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
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


    }


    public static string DoSampleTestTriangle()
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

        return text;



    }


    public static string DoSampleTestVps()
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
        return text;

    }

    public static string DoSampleTest2()
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

        return text;

    }
}
