using Engine.Parser;
using Engine.Sample;

namespace Engine
{
    internal static class ParserTest
    {
        public static string SafetyValid = @"
[sys] L = {
    [flow] F = {
        A.m > Main2;
        A.p > Main;

        Main = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
        Main2 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [jobs] = {
        F.A.m = { A.ADV; }
        F.A.p = { A.RET; }
    }
    [prop] = {
        [safety] = {
            F.Main.A.p = { F.A.m; }
        }
    }
    [device file=""./dsLib/Cylinder/DoubleCylinder.ds""]  A;
}

";
    public static string AutoPreValid = @"
[sys] HelloDS = {
    [flow] STN2 = {
        Work1 = {
            Device11111.ADV > Device11111.RET > Device11111_ADV_1 > Device11111_RET_1;
        }
        [aliases] = {
            Work1.Device11111.ADV = { Device11111_ADV_1; }
            Work1.Device11111.RET = { Device11111_RET_1; }
        }
    }
    [jobs] = {
        STN2.Device11111.ADV[N3(2, 2)] = { STN2_Device11111_01.ADV(IB0.0, OB0.0); STN2_Device11111_02.ADV(IB0.2, OB0.2); STN2_Device11111_03.ADV(-, -); }
        STN2.Device11111.RET[N3(3, 1)] = { STN2_Device11111_01.RET(IB0.1, OB0.1); STN2_Device11111_02.RET(IB0.3, -); STN2_Device11111_03.RET(IB0.4, -); }
    }
    [prop] = {
        [autopre] = {
            STN2.Work1.Device11111.ADV = { STN2.Device11111.RET; }
        }
        [layouts] = {
            STN2_Device11111_01 = (1369, 815, 220, 80);
            STN2_Device11111_02 = (1369, 815, 220, 80);
            STN2_Device11111_03 = (1369, 815, 220, 80);
        }
    }
    [device file=""./dsLib/Cylinder/DoubleCylinder.ds""]
        STN2_Device11111_01,
        STN2_Device11111_02,
        STN2_Device11111_03;
}
";


        public static string LayoutValid = @"
[sys] L = {
    [flow] F = {
        A.m > Main2;
        A.p > Main;

        Main = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
        Main2 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [jobs] = {
        F.A.m = { A.""-""(%I2, %Q2); }
        F.A.p = { A.""+""(%I1, %Q1); }
        F.B.m = { B.""-""(%I4, %Q4); }
        F.B.p = { B.""+""(%I3, %Q3); }
    }
    [prop] = {
        [layouts] = {
            A = (945, 123, 45, 67);
        }
    }
    [device file=""cylinder.ds""] A, B; // D:\ds\dsA\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
}

";
        public static string FinishValid = @"
[sys] L = {
    [flow] F = {
        A.m > Main2;
        A.p > Main;

        Main = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
        Main2 = {
            A.p > A.m;		// A.p(Call)> A.m(Call);
        }
    }
    [jobs] = {
        F.A.m = { A.""-""(%I2, %Q2); }
        F.A.p = { A.""+""(%I1, %Q1); }
    }
    [prop] = {
        [finish] = {
            F.Main2;
            F.Main;
        }
    }
    [device file=""cylinder.ds""] A; // D:\ds\dsA\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
}

";
        public static string DisableValid = @"
[sys] HelloDS = {
    [flow] STN1 = {
        외부시작.""ADV(INTrue)"" > Work1_1;
        Work2 => Work1 => Work2;
        Work1 = {
            Device1.ADV > Device2.ADV;
            Device2_ADV_1;
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work1.Device2.ADV = { Device2_ADV_1; }
        }
    }
    [jobs] = {
        STN1.외부시작.""ADV(INTrue)"" = { STN1_외부시작.ADV(IB0.2, -); }
        STN1.Device1.ADV = { STN1_Device1.ADV(IB0.0, OB0.0); }
        STN1.Device2.ADV = { STN1_Device2.ADV(IB0.1, OB0.1); }
    }

    [prop] = {
        [disable] = {
            STN1.Work1.""Device2.ADV"";
        }
    }
    [device file=""./dsLib/Cylinder/DoubleCylinder.ds""]
        STN1_외부시작,
        STN1_Device1,
        STN1_Device2;
}
//DS Language Version = [1.0.0.1]
//DS Library Date = [Library Release Date 24.3.26]
//DS Engine Version = [0.9.9.1]

";


        public static string Buttons = @"
[sys] HelloDS_DATA = {
    [flow] f1 = {
        Work1 > Work2;
    }

    [buttons] = {
        [a] = {  AutoSelect(_, -) = { f1; } }
        [m] = {  ManualSelect(_, -) = {  f1; } }
        [d] = {  DrivePushBtn(_, -) = {  f1; } }
        [e] = {  EmergencyBtn(_, -) = { f1; } }
        [p] = {  PausePushBtn(_, -) = { f1; } }
        [c] = {  ClearPushBtn(_, -) = { f1; } }
        }
    }

";

        public static string Lamps = @"
[sys] HelloDS_DATA = {
    [flow] f1 = {
        Work1 > Work2;
    }

    [lamps] = {
        [a] = { AutoModeLamp(-, _) = { } }
        [m] = { ManualModeLamp(-, _) = {  } }
        [d] = { DriveLamp(-, _) = {  } }
        [e] = { ErrorLamp(-, _) = {  } }
        [r] = { ReadyStateLamp(-, _) = {  } }
        [i] = { IdleModeLamp(-, _) = { } }
        [o] = { OriginStateLamp(-, _) = {  } }
    }
}
";

        public static string Conditions = @"
[sys] HelloDS_DATA = {
    [flow] f1 = {
        Work1 > Work2;
    }

	[conditions] = {
        [r] = {
            f1_Condition1(_, _) = { f1; }
            f1_Condition2(_, _) = { f1; }
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
            C1 <|> C4;
            C1 > C4;
        }
        R1 = {
            C1 <|> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <|> C2;
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
            C1 <|> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <|> C2;
        }
        R2 = {
            C1 <|> C4;
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
            C1 <|> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <|> C2;
        }
        R2 = {
            C1 <|> C4;
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
            C1 <|> C4;
            C1 > C4;
        }
        R1 = {
            C1 <|> C4;
            C1 > C2;
            C1 > C3;
            C2, C3 > C4;
            C3 <|> C2;
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
            CC1 <|> CC2;
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



        public static string Error = @"
[sys] MY = {
    [flow] Rear = {
        제품공급 = {
            Rear_Con_W > Rear_Pos_Sen;
            Rear_Cyl_Push_ADV > Rear_Cyl_Push_RET;
            Rear_Cyl_Push_RET <|> Rear_Cyl_Push_ADV;
            Rear_Pos_Sen > Rear_Cyl_Push_ADV;
        }
        Rear_Cyl_Push_ADV = {EX.Rear_Rear_Cyl_Push_ADV.TX    ~    EX.Rear_Rear_Cyl_Push_ADV.RX}
        Rear_Cyl_Push_RET = {EX.Rear_Rear_Cyl_Push_RET.TX    ~    EX.Rear_Rear_Cyl_Push_RET.RX}
        Rear_Con_W        = {EX.Rear_Rear_Con_W.TX    ~    _}
        Rear_Pos_Sen      = {_    ~    EX.Rear_Rear_Pos_Sen.RX}
    }
    [flow] Work = {
        작업공정 = {
            Front_1Stopper_Adv <|> Front_1Stopper_RET;
            Front_1Stopper_Adv > Front_1pos_Sen;
            Front_1pos_Sen > Front_Usb_Cyl_ADV;
            Front_Con_W > Front_1Stopper_Adv;
            Front_Pos_Sen > Front_Con_W;
            Front_Usb_Cyl_ADV <|> Front_Usb_Cyl_RET;
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
            Run.R > Run.L;
        }
    }

    [jobs] = {
        F.Run.R = { sysR.RUN(%I1, %Q1); }
        F.Run.L = { sysL.RUN(%I2, %Q2); }
    }
    [external file=""systemRH.ds""] sysR;
    [external file=""systemLH.ds""] sysL;
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
[sys] HelloDS = {
    [flow] STN1 = {
        외부시작.""ADV(INTrue)"" > Work1_1;
        Work2 => Work1 => Work2;
        Work1 = {
            Device1.ADV > Device2.ADV;
            Device2_ADV_1;
        }
        [aliases] = {
            Work1 = { Work1_1; }
            Work1.Device2.ADV = { Device2_ADV_1; }
        }
    }
    [jobs] = {
        STN1.외부시작.""ADV(INTrue)"" = { STN1_외부시작.ADV(IB0.2, -); }
        STN1.Device1.ADV = { STN1_Device1.ADV(IB0.0, OB0.0); }
        STN1.Device2.ADV = { STN1_Device2.ADV(IB0.1, OB0.1); }
    }

    [device file=""./dsLib/Cylinder/DoubleCylinder.ds""]
        STN1_외부시작,
        STN1_Device1,
        STN1_Device2;
}

";


        public static string Diamond = @"
[sys] L = {
    [flow] F = {
        Main = {
            // 정보로서의 Call 상호 리셋
            A.p <|> A.m;
            B.p <|> B.m;
            A.p > A.m, B.p > B.m > A.p1 > A.m1, B.p1 > B.m1;
        }
        F.A.p = {A.F.Vp ~ A.F.Sp}
        F.A.m = {A.F.Vm ~ A.F.Sm}
        F.B.p = {B.F.Vp ~ B.F.Sp}
        F.B.m = {B.F.Vm ~ B.F.Sm}
        [alias] = {
            F.A.p = { A.p1; A.p2; }
            F.A.m = { A.m1; A.m2; }
            F.B.p = { B.p1; B.p2; }
            F.B.m = { B.m1; B.m2; }
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
    F1.C1 = (1309,405,205,83);
    F1.ER2 = (571,803,173,58);
    F1.ER1 = (297,441,173,58);
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



        public static string TaskDevParam = @"
    [sys] Control = {
    [flow] F = {
        #OP1, #OP2 > Main <|> Reset;
        Main = {
            mv1.up > mv1.dn;
        }
    }
    [jobs] = {
        F.mv1.up = { A.""+""(%I300:symbol1:UInt16, %Q300:ABC:Boolean); }
        F.mv1.dn = { A.""-""(%I301, %Q301:0f); }
    }
    [variables] = {}
    [operators] = {
        OP1 = #{$ABC == false;}
        OP2 = #{$symbol1 > 12us;}
    }
    [device file=""cylinder.ds""] A;
}
";

        public static string Operators = @"
    [sys] Control = {
    [flow] F = {
        #OP1, #OP2 > Main <|> Reset;
        Main = {
            mv1.up > mv1.dn;
        }
    }
    [jobs] = {
        F.mv1.up = { A.""+""(%I300:symbol1:UInt16, %Q300); }
        F.mv1.dn = { A.""-""(%I301, %Q301); }
    }
    [variables] = {}
    [operators] = {
        OP1 = #{$symbol1 == 0us;}
        OP2 = #{$symbol1 > 12us;}
    }
    [device file=""cylinder.ds""] A;
}
";

        public static string Commnads = @"
    [sys] Control = {
    [flow] F = {
        Main <|> Reset;	
        Main = {
            mv1.up > mv1.dn > CMD1(), CMD2();
        }
    }
    [jobs] = {
        F.mv1.up = { A.""+""(%I300, %Q300); }
        F.mv1.dn = { A.""-""(%I301, %Q301:AOUT:Int32:300); }
    }
    [variables] = {
        Int32 v0;
    }
    [commands] = {
        CMD1 = #{$v0 = 1;}
        CMD2 = #{$AOUT = 2;}
    }
    [device file=""cylinder.ds""] A; // Z:/ds/DsDotNet/src/UnitTest/UnitTest.Model/UnitTestExA.mple/dsSimple/cylinder.ds
}
";


        public static string Times = @"
    [sys] Control = {
    [flow] F = {
        Work1 > Work2 > Work3;
    }

    [prop] = {
        [times] = {
                F.Work1 = {AVG(2)};
                F.Work2 = {AVG(1)};
                F.Work3 = {AVG(0.1), STD(1)};
                   }
        }
    }
";

        public static string Motions = @"
    [sys] Control = {
    [flow] F = {
        Work1 > Work2 > Work3;
    }

    [prop] = {
        [motions] = {
            F.Work1 = {./Assets/Cylinder/DoubleType.obj:RET};
            F.Work2 = {./Assets/Cylinder/DoubleType.obj:ADV};
            }
        }
    }

";

        public static string Scripts = @"
    [sys] Control = {
    [flow] F = {
        Work1 > Work2 > Work3;
    }

    [prop] = {
        [scripts] = {
            F.Work1 = {scripsPath1};
            F.Work2 = {scripsPath2.sc};
            F.Work3 = {scripsDir/scripsPath3.sc};
            }
        }
    }

";

        public static string Repeats = @"
    [sys] Control = {
    [flow] F = {
        Work1 > Work2 > Work3;
    }
    [prop] = {
        [repeats] = {
                F.Work1 = {2};
                F.Work3 = {3};
            }
        }
    }
"; public static string Errors = @"
    [sys] HelloDS = {
        [flow] STN1 = {
            Work2 => Work1;
            Work1 = {
                Device1.ADV > Device2.ADV;
            }
        }
        [jobs] = {
            STN1.Device1.ADV = { STN1_Device1.ADV(IB0.0, OB0.0); }
            STN1.Device2.ADV = { STN1_Device2.ADV(IB0.1, OB0.1); }
        }
        [prop] = {
        
            [errors] = {
                STN1.Device1.ADV = {MIN(0.01), MAX(1.2), CHK(0.5)};
                STN1.Device2.ADV = {CHK(0.5)};
            }
        }
        [device file=""./dsLib/Cylinder/DoubleCylinder.ds""]
            STN1_외부시작,
            STN1_Device1,
            STN1_Device2;
    }
";

    }
}
