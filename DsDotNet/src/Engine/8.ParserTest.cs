using Engine.Parser;

namespace Engine
{
    internal static class ParserTest
    {
        public static void TestParseSafety()
        {
            var text = @"
[sys] L = {
    [flow] F = {
        Main = { T.Cp > T.Cm; }
        [safety] = {
            Main = {P.F.Sp; P.F.Sm}
            Main2 = {P.F.Sp; P.F.Sm}
        }
    }
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
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

[prop] = {
    [ safety ] = {
        L.F.Main = {P.F.Sp; P.F.Sm}
        L.F.Main2 = {P.F.Sp; P.F.Sm}
    }
}
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] PCpu = {
        P.F;
    }
}

";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine;
            Program.Engine = engine;
            engine.Run();
        }
        public static void TestParseStrongCausal()
        {
            var text = @"
[sys] L = {
    [flow] F = {
        Main = {
            Cp >> Cm;
            Cp ||> Cm;
            Cp <|| Cm;
        }
        [task] = {
            Cp = {P.F.Vp ~ P.F.Sp}
            Cm = {P.F.Vm ~ P.F.Sm}
        }
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
    [cpu] PCpu = {
        P.F;
    }
}

";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine;
            Program.Engine = engine;
            engine.Run();
        }

        public static void TestParseButtons()
        {
            var text = @"
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
    [emg] = {
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

[cpus] AllCpus = {
    [cpu] Cpu = {
        My.F1;
        My.F2;
        My.F3;
        My.F4;
        My.F5;
    }
}

";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine;
            Program.Engine = engine;
            engine.Run();
        }

        public static void TestParsePpt()
        {
            var text = @"
//////////////////////////////////////////////////////
//DTS model auto generation from D:\DS\test\DS.pptx
//////////////////////////////////////////////////////
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
[cpus] AllCpus = {
    [cpu] Cpu = {
        MY.F1;
        MY.F2;
        MY.F3;
        MY.F5;
        MY.FF;
    }
}
";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine;
            Program.Engine = engine;
            engine.Run();
        }

        public static void TestSerialize()
        {
            var text = @"
[sys] L = {
    [flow] F = {
        Main = { Cp > Cm; }
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
        Cx = {P.F.Vm ~ _}
    }

    [start] = {
        StartBTN_FF = { F };
        StartBTN1 = { F; };
    }
    [reset] = {
        ResetBTN = { F };
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
    [cpu] PCpu = {
        P.F;
    }
}

";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine;
            Program.Engine = engine;
            engine.Run();
        }

        public static void TestError()
        {
            var text = @"
[sys] MY = {
    [flow] Rear = {     
        제품공급 = {
            Rear_Con_W > Rear_Pos_Sen;
            Rear_Cyl_Push_ADV > Rear_Cyl_Push_RET;
            Rear_Cyl_Push_RET <||> Rear_Cyl_Push_ADV;
            Rear_Pos_Sen > Rear_Cyl_Push_ADV;
        }
        Rear_Cyl_Push_ADV     = {EX.Rear_Rear_Cyl_Push_ADV.TX    ~    EX.Rear_Rear_Cyl_Push_ADV.RX}
        Rear_Cyl_Push_RET     = {EX.Rear_Rear_Cyl_Push_RET.TX    ~    EX.Rear_Rear_Cyl_Push_RET.RX}
        Rear_Con_W     = {EX.Rear_Rear_Con_W.TX    ~    _}
        Rear_Pos_Sen     = {_    ~    EX.Rear_Rear_Pos_Sen.RX}
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
        Front_Usb_Cyl_RET     = {EX.Work_Front_Usb_Cyl_RET.TX    ~    EX.Work_Front_Usb_Cyl_RET.RX}
        Front_Con_W     = {EX.Work_Front_Con_W.TX    ~    _}
        Front_1Stopper_Adv     = {EX.Work_Front_1Stopper_Adv.TX    ~    EX.Work_Front_1Stopper_Adv.RX}
        Front_Pos_Sen     = {_    ~    EX.Work_Front_Pos_Sen.RX}
        Front_1Stopper_RET     = {EX.Work_Front_1Stopper_RET.TX    ~    EX.Work_Front_1Stopper_RET.RX}
        Front_Usb_Cyl_ADV     = {EX.Work_Front_Usb_Cyl_ADV.TX    ~    EX.Work_Front_Usb_Cyl_ADV.RX}
        Front_1pos_Sen     = {_    ~    EX.Work_Front_1pos_Sen.RX}
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



[cpus] AllCpus = {
    [cpu] Cpu_MY = {
        MY.Rear;
        MY.Work;
        MY.Model_Auto;
    }
    [cpu] Cpu_EX = {
        EX.Rear_Rear_Cyl_Push_ADV;
        EX.Rear_Rear_Cyl_Push_RET;
        EX.Rear_Rear_Con_W;
        EX.Rear_Rear_Pos_Sen;
        EX.Work_Work;
        EX.Work_Front_Usb_Cyl_RET;
        EX.Work_Front_Con_W;
        EX.Work_Front_1Stopper_Adv;
        EX.Work_Front_Pos_Sen;
        EX.Work_Front_1Stopper_RET;
        EX.Work_Front_Usb_Cyl_ADV;
        EX.Work_Front_1pos_Sen;
    }
}
[addresses] = {
    EX.Rear_Rear_Cyl_Push_ADV.TX                 = (, , )
    EX.Rear_Rear_Cyl_Push_ADV.RX                 = (, ,)
    EX.Rear_Rear_Cyl_Push_RET.TX                 = (, , )
    EX.Rear_Rear_Cyl_Push_RET.RX                 = (, ,)
    EX.Rear_Rear_Con_W.TX                        = (, , )
    EX.Rear_Rear_Pos_Sen.RX                      = (, ,)
    EX.Work_Work.TR                              = (,,)
    EX.Work_Front_Usb_Cyl_RET.TX                 = (, , )
    EX.Work_Front_Usb_Cyl_RET.RX                 = (, ,)
    EX.Work_Front_Con_W.TX                       = (, , )
    EX.Work_Front_1Stopper_Adv.TX                = (, , )
    EX.Work_Front_1Stopper_Adv.RX                = (, ,)
    EX.Work_Front_Pos_Sen.RX                     = (, ,)
    EX.Work_Front_1Stopper_RET.TX                = (, , )
    EX.Work_Front_1Stopper_RET.RX                = (, ,)
    EX.Work_Front_Usb_Cyl_ADV.TX                 = (, , )
    EX.Work_Front_Usb_Cyl_ADV.RX                 = (, ,)
    EX.Work_Front_1pos_Sen.RX                    = (, ,)
    //EX.AutoBTN.RX                                = (, ,)
    //EX.EMGBTN.RX                                 = (, ,)
    //EX.ResetBTN.RX                               = (, ,)
    //EX.StartBTN1.RX                              = (, ,)
}

";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu")).Engine;
            Program.Engine = engine;
            engine.Run();
        }

        public static void TestParseQualifiedName()
        {
            var text = @"
[sys] ""my.favorite.system!!"" = {
    [flow] "" my flow. "" = {
        R1 > R2;
        C1     = {EX.""이상한. flow"".TX    ~    EX.""이상한. flow"".RX}
    }
}
[sys] EX = {
    [flow] ""이상한. flow"" = {    
        TX;
        RX;
    }
}
";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation()).Engine;
            Program.Engine = engine;
            engine.Run();
        }

        public static void TestParseExternalSegmentCall()
        {
            var text = @"
[sys] MY = {
    [flow] FFF = {     
        EX.""FFF.EXT"".EX > R2;
    }
}

[sys] EX = {
    [flow] ""FFF.EXT"" = { EX; }
}
";
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation()).Engine;
            Program.Engine = engine;
            engine.Run();
        }


    }
}
