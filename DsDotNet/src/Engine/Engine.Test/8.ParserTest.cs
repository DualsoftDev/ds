using Engine.Parser;
using Engine.Sample;

namespace Engine
{
    internal static class ParserTest
    {
        public static string SafetyValid = @"
[sys] L = {
    [flow] F = {
        Main = { Cp > Cm; }
        [aliases] = {
            C.P = { Cp; Cp1; Ap2; }
            C.M = { Cm; Cm1; Cm2; }
        }
        // ---- flow 내의 safety block 없애는 걸로...
        //[safety] = {
        //    Main = {C.F.Sp; C.F.Sm}
        //}
    }
    [sys] C = {
        [flow] F = {
            Vp > Pp > Sp;
            Vm > Pm > Sm;

            Pp |> Sm;
            Pm |> Sp;
            Vp <||> Vm;
        }
        [interfaces] = {
            P = { F.Vp ~ F.Sp }
            M = { F.Vm ~ F.Sm }
            // 정보로서의 상호 리셋
            P <||> M;
        }
    }

    [prop] = {
        [addresses] = {
            C.P = ( %Q1234.2343, %I1234.2343)
            C.M = ( START, END)
        }
    }
    [prop] = {
        [ safety ] = {
            L.F.Main = {C.F.Vp; C.F.Vm}
        }
    }
}

";

        public static string SafetyDuplicatedInvalid = @"
[sys] L = {
    [flow] F = {
        Main = { Cp > Cm; }
        [aliases] = {
            C.P = { Cp; Cp1; Ap2; }
            C.M = { Cm; Cm1; Cm2; }
        }
        [safety] = {
            Main = {C.F.Sp; C.F.Sm}
        }
    }
}

[sys] C = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Pp |> Sm;
        Pm |> Sp;
        Vp <||> Vm;
    }
    [interfaces] = {
        P = { F.Vp ~ F.Sp }
        M = { F.Vm ~ F.Sm }
        // 정보로서의 상호 리셋
        P <||> M;
    }
}

[prop] = {
    [ safety ] = {
        L.F.Main = {C.F.Sp; C.F.Sm}
    }
}
";

        public static string StrongCausal = @"
[sys] L = {
    [flow] F = {
        Main = {
            Cp >> Cm;
            Cp ||> Cm;
            Cp <|| Cm;
        }
        [aliases] = {
            A.P = { Cp; Cp1; Ap2; }
            A.M = { Cm; Cm1; Cm2; }
        }
    }
    [sys] A = {
        [flow] F = {
            Vp > Pp > Sp;
            Vm > Pm > Sm;

            Pp |> Sm;
            Pm |> Sp;
            Vp <||> Vm;
        }
        [interfaces] = {
            P = { F.Vp ~ F.Sp }
            M = { F.Vm ~ F.Sm }
            // 정보로서의 상호 리셋
            P <||> M;
        }
    }
    [prop] = {
        [addresses] = {
            A.P = ( %Q1234.2343, %I1234.2343)
            A.M = ( START, END)
        }
    }
}
";

        public static string Buttons = @"
[sys] My = {
    [flow] F1 = { A > B; }
    [flow] F2 = { A > B; }
    [flow] F3 = { A > B; }
    [flow] F4 = { A > B; }
    [flow] F5 = { A > B; }
    [emg] = {
        EmptyButton = {};
        EmptyButton2 = {}
        EMGBTN3 = { F3; F5 };
        EMGBTN = { F1; F2; F3; F5; };
    }
    [auto] = {
        //AutoBTN2;     Empty not allowed
        AutoBTN = { F2 };
        AutoBTN2 = { F1; F3; F5; };
    }
    [start] = {
        StartBTN_FF = { F2 };
        StartBTN1 = { F1; };
    }
    [reset] = {
        ResetBTN = { F1; F2; F3; F5; };
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
    [emg] = {
        EMGBTN3 = { F3; F5 };
        EMGBTN = { F1; F2; F3; F5; FF };
    }
    [auto] = {
        //AutoBTN2;     Empty not allowed
        //AutoBTN = { F2 };
        AutoBTN = { F1; F3; F5; FF };
    }
    [start] = {
        StartBTN_FF = { FF };
        StartBTN1 = { F1 };
    }
    [reset] = {
        ResetBTN = { F1; F2; F3; F5; FF };
    }
}
";

        public static string Dup = @"
[sys] L = {
    [flow] FF = {
        A, ""F2.R2"" > C;
        C |> ""F2.R2"";
    }
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
    [emg_in] = {
        EMGBTN = { Work; Model_Auto };
    }
    [auto_in] = {
        AutoBTN = { Work; Model_Auto };
    }
    [start_in] = {
        StartBTN1 = { Work; Model_Auto };
    }
    [reset_in] = {
        ResetBTN = { Work; Model_Auto };
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


[addresses] = {
    EX.Rear_Rear_Cyl_Push_ADV.TX  = (, , )
    EX.Rear_Rear_Cyl_Push_ADV.RX  = (, ,)
    EX.Rear_Rear_Cyl_Push_RET.TX  = (, , )
    EX.Rear_Rear_Cyl_Push_RET.RX  = (, ,)
    EX.Rear_Rear_Con_W.TX         = (, , )
    EX.Rear_Rear_Pos_Sen.RX       = (, ,)
    EX.Work_Work.TR               = (,,)
    EX.Work_Front_Usb_Cyl_RET.TX  = (, , )
    EX.Work_Front_Usb_Cyl_RET.RX  = (, ,)
    EX.Work_Front_Con_W.TX        = (, , )
    EX.Work_Front_1Stopper_Adv.TX = (, , )
    EX.Work_Front_1Stopper_Adv.RX = (, ,)
    EX.Work_Front_Pos_Sen.RX      = (, ,)
    EX.Work_Front_1Stopper_RET.TX = (, , )
    EX.Work_Front_1Stopper_RET.RX = (, ,)
    EX.Work_Front_Usb_Cyl_ADV.TX  = (, , )
    EX.Work_Front_Usb_Cyl_ADV.RX  = (, ,)
    EX.Work_Front_1pos_Sen.RX     = (, ,)
}

";

        public static string QualifiedName = @"
[sys] ""my.favorite.system!!"" = {
    [flow] "" my flow. "" = {
        R1 > R2;
        C1 = {
            EX.""이상한. Api"" >
            EX.""Dummy. Api""
            // > EX.""이상한. Api""
            ; }
    }
    [sys] EX = {
        [flow] F = {
            TX;
            ""R.X"";
            ""NameWith\""Quote"";
        }
        [interfaces] = {
            ""이상한. Api"" = { F.TX ~ F.""R.X"" }
            ""Dummy. Api"" = { _ ~ _ }
        }
    }
}
";
        public static string T6Alias = @"
[sys ip = localhost] T6_Alias = {
    [flow] Page1 = {
    }
    [flow] AndFlow = {
        R2 > R3;
        R1 > R3;
    }
    [flow] OrFlow = {
        R2 > Copy1_R3;
        R1 > R3;
        [aliases] = {
            R3 = { Copy1_R3; }
        }
    }
}
";

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
            Ap1 > Am1, Ap2 > Am2;
        }
        [aliases] = {
            A.""+"" = { Ap1; Ap2; Ap3; }
            A.""-"" = { Am1; Am2; Am3; }    // system name optional
            //Vp = {AVp1;}  // invalid: 자신 시스템에 정의된 것만 alias
        }
    }
    [prop] = {
        [addresses] = {
            A.""+"" = (%Q1234.2343, %I1234.2343)
            A.""-"" = (START, END)
        }
    }
    " + SampleRunner.CreateCylinder("A") + @"
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

[addresses] = {
    EX.F1_ER1.EXT = (,,)
    EX.F1_ER2.EXT = (,,)
    EX.F1_C1.TX   = (, , )
    EX.F1_C1.RX   = (, ,)
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
