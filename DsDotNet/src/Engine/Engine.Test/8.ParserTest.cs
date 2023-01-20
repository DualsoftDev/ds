using Engine.Parser;
using Engine.Sample;

namespace Engine
{
    internal static class ParserTest
    {
        public static string SafetyValid = @"
[sys] L = {
    [flow] F = {
        Ap > Am;
        Main = {
            Ap1 > Am1;
        }
        [aliases] = {
            Ap = { Ap1; Ap2; Ap3; }
            Am = { Am1; Am2; Am3; }
        }
        // ---- flow 내의 safety block 없애는 걸로...
        //[safety] = {
        //    Main = {C.F.Sp; C.F.Sm}
        //}
    }
    [jobs] = {
        Ap = { A.""+""(%I1, %Q1); }
        Am = { A.""-""(%I2, %Q2); }
    }

    [prop] = {
        [safety] = {
            F.Main = { F.Ap; F.Am; }
            F.Ap = { F.Main; }
        }
    }

    [device file=""cylinder.ds""] A;
}

";
        public static string StrongCausal = @"
[sys] L = {
    [flow] F = {
        Main = {
            Ap >> Am;
            Ap ||> Am;
            Ap <|| Am;
        }
    }
    [jobs] = {
        Ap = { A.""+""(%I1, %Q1); }
        Am = { A.""-""(%I2, %Q2); }
    }
    [device file=""cylinder.ds""] A;
}
";

        public static string Buttons = @"
[sys] My = {
    [flow] F1 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F2 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F3 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F4 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F5 = {
        A > B;		// A(Real)> B(Real);
    }
    [buttons] = {
        [a] = {
            AutoBTN(%I1, %Q1) = { F2; }
            AutoBTN2(%I2, %Q2) = { F1;F3;F5; }
        }
        [m] = {
            ManualBTN(_, _) = { F1;F5; }
        }
        [e] = {
            EmptyButton(_, _) = {}
            EmptyButton2(_, _) = {}
            EMGBTN3(_, _) = { F3;F5; }
            EMGBTN(_, _) = { F1;F2;F3;F5; }
        }
        [s] = {
            StopBTN(_, _) = { F1;F2;F5; }
        }
        [d] = {
            StartBTN_FF(_, _) = { F2; }
            StartBTN1(_, _) = { F1; }
        }
        [t] = {
            StartTestBTN(_, _) = { F5; }
        }
        [c] = {
            ClearBTN(_, _) = { F1;F2;F3;F5; }
        }
        [h] = {
            HomeBTN(_, _) = { F1;F2;F3;F5; }
            HomeBTN.func = {
                $TON 2000;
                $CTU 1 5;
            }
        }
    }
}
";

        public static string Lamps = @"
[sys] My = {
    [flow] F1 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F2 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F3 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F4 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F5 = {
        A > B;		// A(Real)> B(Real);
    }
    [lamps] = 
    {
        [a] = {
            AutoMode(%Q1) = { F1 }
        }
        [m] = {
            ManualMode(%Q1) = { F2 }
        }
        [d] = {
            RunMode(%Q1) = { F3  }
        }
        [s] = {
            StopMode(%Q1) = { F3 }
        }
        [e] = {
            EmgMode(%Q1) = { F3 }
        }
        [t] = {
            TestMode = { F5 }
        }
        [r] = {
            ReadyMode = { F4 }
            ReadyMode.func = {
                $TON 2000;
                $CTU 1 5;
            }
        }
    }
}
";

        public static string Conditions = @"
[sys] My = {
    [flow] F1 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F2 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F3 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F4 = {
        A > B;		// A(Real)> B(Real);
    }
    [flow] F5 = {
        A > B;		// A(Real)> B(Real);
    }
    [conditions] = 
    {
        [d] = {
            AirOn1(%I1) = { F1;F2; }
            AirOn2(%I2) = { F1; }
        }
        [r] = {
            LeakErr(%I3) = { F2; }
            LeakErr.func = {
                $TON 2000;
                $CTU 1 5;
            }
        }
    }
}
";
        public static string Ppt = @"
[sys] MY = {
    [flow] F1 = {
        R1 > R2;
        R2 |> R1;
        R2 = {
            C1 <||> C4;
            C1 > C4;
        }
        R1 = {
            C1 <||> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <||> C2;
        }
        C1     = {EX.F1_C1.TX    ~    EX.F1_C1.RX}
        C4     = {EX.F1_C4.TX    ~    EX.F1_C4.RX}
        C2     = {EX.F1_C2.TX    ~    EX.F1_C2.RX}
        C3     = {EX.F1_C3.TX    ~    EX.F1_C3.RX}
        ResetBTN     = {EX.F1_ResetBTN.TX    ~    EX.F1_ResetBTN.RX}
    }
    [flow] F2 = {
        R1 > R2;
        R2 |> R1;
        R1 = {
            C1 <||> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <||> C2;
        }
        R2 = {
            C1 <||> C4;
            C1 > C4;
        }
        C1     = {EX.F2_C1.TX    ~    EX.F2_C1.RX}
        C4     = {EX.F2_C4.TX    ~    EX.F2_C4.RX}
        C3     = {EX.F2_C3.TX    ~    EX.F2_C3.RX}
        C2     = {EX.F2_C2.TX    ~    EX.F2_C2.RX}
        ResetBTN     = {EX.F2_ResetBTN.TX    ~    EX.F2_ResetBTN.RX}
    }
    [flow] F3 = {
        R1 > R2;
        R2 |> R1;
        R1 = {
            C1 <||> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <||> C2;
        }
        R2 = {
            C1 <||> C4;
            C1 > C4;
        }
        C1     = {EX.F3_C1.TX    ~    EX.F3_C1.RX}
        C2     = {EX.F3_C2.TX    ~    EX.F3_C2.RX}
        C4     = {EX.F3_C4.TX    ~    EX.F3_C4.RX}
        C3     = {EX.F3_C3.TX    ~    EX.F3_C3.RX}
        ResetBTN     = {EX.F3_ResetBTN.TX    ~    EX.F3_ResetBTN.RX}
    }
    [flow] F5 = {
        R1 > R2;
        R2 |> R1;
        R2 = {
            C1 <||> C4;
            C1 > C4;
        }
        R1 = {
            C1 <||> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <||> C2;
        }
        C1     = {EX.F5_C1.TX    ~    EX.F5_C1.RX}
        C4     = {EX.F5_C4.TX1, EX.F5_C4.TX2, EX.F5_C4.TX3, EX.F5_C4.TX4, EX.F5_C4.TX5    ~    EX.F5_C4.RX1, EX.F5_C4.RX2, EX.F5_C4.RX3, EX.F5_C4.RX4, EX.F5_C4.RX5, EX.F5_C4.RX6}
        C2     = {EX.F5_C2.TX    ~    EX.F5_C2.RX}
        C3     = {EX.F5_C3.TX    ~    EX.F5_C3.RX}
        ResetBTN     = {EX.F5_ResetBTN.TX    ~    EX.F5_ResetBTN.RX}
    }
    [flow] FF = {
        ""F1.R2"", ""F2.R2"", ""F3.R2"", ""F5.C4"", ""F5.R2"" > RR1;
        RR1 |> ""F2.R2"";
        RR1 |> ""F3.R2"";
        RR1 |> ""F1.R2"";
        RR1 = {
            CC1 <||> CC2;
            CC1 > CC2;
        }
        CC1     = {EX.FF_CC1.TX    ~    EX.FF_CC1.RX}
        CC2     = {EX.FF_CC2.TX    ~    EX.FF_CC2.RX}
        ResetBTN     = {EX.FF_ResetBTN.TX    ~    EX.FF_ResetBTN.RX}
    }
    [buttons] = {
        [e] = {
            EMGBTN3 = { F3; F5 };
            EMGBTN = { F1; F2; F3; F5; FF };
        }
        [a] = {
            //AutoBTN2;     Empty not allowed
            //AutoBTN = { F2 };
            AutoBTN = { F1; F3; F5; FF };
        }
        [d] = {
            StartBTN_FF = { FF };
            StartBTN1 = { F1 };
        }
    }
    [reset] = {
        ResetBTN = { F1; F2; F3; F5; FF };
    }
}
";

        public static string Dup = @"
[sys] L = {
    [flow] FF = {
        A, Ap > C |> Ap;
    }
    [jobs] = {
        Ap = { A.""+""(%I1, %Q1); }
        //Am = { A.""-""(%I2, %Q2); }  //사용 안되면 정의 불가
    }
    [device file=""cylinder.ds""] A;
}
";


        public static string Error = @"
[sys] MY = {
    [flow] Rear = {
        제품공급 = {
            Rear_Con_W > Rear_Pos_Sen;
            Rear_Cyl_Push_ADV > Rear_Cyl_Push_RET;
            Rear_Cyl_Push_RET <||> Rear_Cyl_Push_ADV;
            Rear_Pos_Sen > Rear_Cyl_Push_ADV;
        }
        Rear_Cyl_Push_ADV = {EX.Rear_Rear_Cyl_Push_ADV.TX    ~    EX.Rear_Rear_Cyl_Push_ADV.RX}
        Rear_Cyl_Push_RET = {EX.Rear_Rear_Cyl_Push_RET.TX    ~    EX.Rear_Rear_Cyl_Push_RET.RX}
        Rear_Con_W        = {EX.Rear_Rear_Con_W.TX    ~    _}
        Rear_Pos_Sen      = {_    ~    EX.Rear_Rear_Pos_Sen.RX}
    }
    [flow] Work = {
        작업공정 = {
            Front_1Stopper_Adv <||> Front_1Stopper_RET;
            Front_1Stopper_Adv > Front_1pos_Sen;
            Front_1pos_Sen > Front_Usb_Cyl_ADV;
            Front_Con_W > Front_1Stopper_Adv;
            Front_Pos_Sen > Front_Con_W;
            Front_Usb_Cyl_ADV <||> Front_Usb_Cyl_RET;
            Front_Usb_Cyl_ADV > EX.Work_Work.TR;
            Front_Usb_Cyl_RET > Front_1Stopper_RET;
            EX.Work_Work.TR > Front_Usb_Cyl_RET;
        }
        Front_Usb_Cyl_RET  = {EX.Work_Front_Usb_Cyl_RET.TX     ~    EX.Work_Front_Usb_Cyl_RET.RX}
        Front_Con_W        = {EX.Work_Front_Con_W.TX           ~    _}
        Front_1Stopper_Adv = {EX.Work_Front_1Stopper_Adv.TX    ~    EX.Work_Front_1Stopper_Adv.RX}
        Front_Pos_Sen      = {_                                ~    EX.Work_Front_Pos_Sen.RX}
        Front_1Stopper_RET = {EX.Work_Front_1Stopper_RET.TX    ~    EX.Work_Front_1Stopper_RET.RX}
        Front_Usb_Cyl_ADV  = {EX.Work_Front_Usb_Cyl_ADV.TX     ~    EX.Work_Front_Usb_Cyl_ADV.RX}
        Front_1pos_Sen     = {_                                ~    EX.Work_Front_1pos_Sen.RX}
    }
    [flow] Model_Auto = {
        SSSS > Rear.제품공급;
        Work.작업공정 > Front.배출공정;
        Rear.제품공급 > Work.작업공정;
    }
    [buttons] = {
        [e_in] = {
            EMGBTN = { Work; Model_Auto };
        }
        [a_in] = {
            AutoBTN = { Work; Model_Auto };
        }
        [d_in] = {
            StartBTN1 = { Work; Model_Auto };
        }
        [c_in] = {
            ResetBTN = { Work; Model_Auto };
        }
    }
}



//////////////////////////////////////////////////////
//DTS auto generation MY ExSegs
//////////////////////////////////////////////////////
[sys] EX = {
    [flow] Rear_Rear_Cyl_Push_RET = { TX > RX <| Rear_Rear_Cyl_Push_ADV.TX; }
    [flow] Rear_Rear_Cyl_Push_ADV = { TX > RX <| Rear_Rear_Cyl_Push_RET.TX; }
    [flow] Rear_Rear_Con_W = { TX > RX; }
    [flow] Rear_Rear_Pos_Sen = { TX > RX; }
    [flow] Work_Front_Usb_Cyl_ADV = { TX > RX <| Work_Front_Usb_Cyl_RET.TX; }
    [flow] Work_Front_Usb_Cyl_RET = { TX > RX <| Work_Front_Usb_Cyl_ADV.TX; }
    [flow] Work_Front_1Stopper_Adv = { TX > RX <| Work_Front_1Stopper_RET.TX; }
    [flow] Work_Front_1Stopper_RET = { TX > RX <| Work_Front_1Stopper_Adv.TX; }
    [flow] Work_Front_Con_W = { TX > RX; }
    [flow] Work_Front_Pos_Sen = { TX > RX; }
    [flow] Work_Front_1pos_Sen = { TX > RX; }
    [flow] Work_Work = { TR; }
}
";

        public static string QualifiedName = @"
[sys] ""my.favorite.system!!"" = {
    [flow] "" my flow. "" = {
        R1 > R2;
        C1 = {
            EX.""이상한. Real"" > EX.""Dummy. Real"";
            // > EX.""이상한. Real""
            }
    }
	[flow] EX = {
		""이상한. Real"" > ""Dummy. Real"";
	}
}
";
        public static string CircularDependency = @"
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;
        Seg1 = {
            RunR > RunL;
        }
    }
    
    [jobs] = {
        RunR = { sysR.RUN(%I1, %Q1); }
        RunL = { sysL.RUN(%I2, %Q2); }
    }
    [external file=""systemRH.ds"" ip=""localhost""] sysR;
    [external file=""systemLH.ds"" ip=""localhost""] sysL;
}
";

        public static string T6Alias = @"
[sys ip = localhost] T6_Alias = {
    [flow] Page1 = {
        AndFlow.R2 > OrFlow.R1;
        C1 > C2;
    }
    [flow] AndFlow = {
        R2 > R3;
        R1 > R3;
    }
    [flow] OrFlow = {
        R2 > Copy1_R3;
        R1 > R3;
        [aliases] = {
            R3 = { Copy1_R3; AliasToR3; }
            AndFlow.R3 = { AndFlowR3; OtherFlowR3; }
        }
    }
    [jobs] = {
        C1 = { B.""+""(%I1, %Q1); A.""+""(_, %Q999.2343); }
        C1.func = {
            $TON 2000;
            $CTU 1 5;
        }
        C2 = { A.""-""(_, %Q3); B.""-""(%I1, _); }
    }
    [external file=""cylinder.ds"" ip=""192.168.0.1""] A;
    [device file=""cylinder.ds""] B;
    // [device file=c:/my.a.b.c.d.e.ds] C;      //<-- illegal: file path without quote!!
}
";


        public static string TaskLinkorDevice = @"
    [sys] Control = {
    [flow] F = {
        Main <||> Reset;		
        FWD <| Main |> BWD;		
        FWD > BWD > Main |> FWD2 |> BWD2;		
        Main = {
            mv1up > mv1dn;		
        }
        [aliases] = {
            FWD = { FWD2; }
            BWD = { BWD2; }
        }
    }
    [jobs] = {
        mv1up = { A.""+""(%I300, %Q300); }
        mv1dn = { A.""-""(%I301, %Q301); }
        FWD = sysR.RUN;
        BWD = sysR.RUN;
    }
    [interfaces] = {
        G = { F.Main ~ F.Main }
        R = { F.Reset ~ F.Reset }
        G <||> R;
    }
    [device file=""cylinder.ds""] A;
    [external file=""systemRH.ds"" ip=""localhost""] sysR;
    [external file=""systemLH.ds"" ip=""localhost""] sysL;
}";

        public static string ExternalSegmentCall = @"
[sys] MY = {
    [flow] FFF = {
        Main = {
            EX.""FFF.EXT"".EX > R2;
        }
        R2 = {_ ~ _}
    }
}

[sys] EX = {
    [flow] ""FFF.EXT"" = { EX; }
}
";

        public static string ExternalSegmentCallConfusing = @"
[sys] MY = {
    [flow] MyOtherFlow = {
        A > B;
    }
    [flow] FFF = {
        Main = {
            MyOtherFlow.A > MyOtherFlow.B;
        }
    }
}
";
        public static string ExternalSegmentCallConfusing2 = @"
[sys] MY = {
    [alias] = {
        MyOtherFlow.A = { MyOtherFlow_A; }
    }
    [flow] MyOtherFlow = {
        A > B;
    }
    [flow] FFF = {
        C > MyOtherFlow_A;
    }
}
";
        public static string MyFlowReference = @"
[sys] EX = {
	[flow] F1_C3 = { TX > RX <| F1_C2.TX; }
	[flow] F1_C2 = { TX > RX <| TX; }
}
";


        public static string Aliases = @"
[sys] my = {
    [flow] F = {
        Main = {
            // AVp1 |> Am1;
            // 정보로서의 Call 상호 리셋
            Ap1 <||> Am1;
            Ap > Am; 
        }
        [aliases] = {
            Main.Ap = { Ap1; Ap2; Ap3; }
            Main.Am = { Am1; Am2; Am3; }    // system name optional
            //Vp = {AVp1;}  // invalid: 자신 시스템에 정의된 것만 alias
        }
    }
    [jobs] = {
        Ap = { A.""+""(%I1, %Q1); }
        Am = { A.""-""(%I2, %Q2); }
    }
    [device file=""cylinder.ds""] A;
}
";


        public static string Diamond = @"
[sys] L = {
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            Ap <||> Am;
            Bp <||> Bm;
            Ap > Am, Bp > Bm > Ap1 > Am1, Bp1 > Bm1;
        }
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
        [alias] = {
            Ap = { Ap1; Ap2; }
            Am = { Am1; Am2; }
            Bp = { Bp1; Bp2; }
            Bm = { Bm1; Bm2; }
        }

    }
}

" + SampleRunner.CreateCylinder("A") + SampleRunner.CreateCylinder("B");

        public static string Call3 = @"
[sys] MY = {
    [flow] F1 = {     
        ER1 > R1;
        R2 = { ER2 > C1; }
        R3 = { C1; }
        R1 = { C1; }
        ER1 = {EX.F1_ER1.EXT ~ EX.F1_ER1.EXT }
        ER2 = {EX.F1_ER2.EXT ~ EX.F1_ER2.EXT }
        C1  = {EX.F1_C1.TX   ~ EX.F1_C1.RX}
    }
}

[sys] EX = {
    [flow] F1_ER1 = { EXT; }
    [flow] F1_ER2 = { EXT; }
    [flow] F1_C1 = { TX > RX; }
}

[layouts] = {
    MY.F1.C1 = (1309,405,205,83)
    MY.F1.ER2 = (571,803,173,58)
    MY.F1.ER1 = (297,441,173,58)
}
"
;

        public static string MyOtherFlowCall = @"
[sys] MY = {
    [flow] F2 = {
        R1;
        F1.R3;          // External segment call
        F1.C2;          // External call call
        F1.C1 > F1.R3;
    }
    [flow] F1 = {
        R3 = { C1; F2.R1; }
        R4;
        C1  = {EX.F1_C1.TX   ~ EX.F1_C1.RX}
        C2  = {EX.F1_C1.TX   ~ EX.F1_C1.RX}
        C;
    }
}
[sys] EX = {
    [flow] F1_C1 = { TX > RX; }
}
";
    }
}
