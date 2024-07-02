namespace Engine.Parser.FS

open Antlr4.Runtime

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser

module Program =
    let CylinderText =
        """
[sys] Cylinder = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <|> Vm;
    }
    [interfaces] = {
        "+" = { F.Vp ~ F.Sp }
        "-" = { F.Vm ~ F.Sm }
        "+" <|> "-";
    }
}
"""

    let EveryScenarioText =
        """
[sys] My = {
    [flow] MyFlow = {
        #STN1_ON > Seg1 > Seg2;
        Seg1 = {
            Ap > Am;
        }
        Seg2 = {
            STN1_COMMAD1(); 
        }
    }
    [flow] "Flow.Complex" = {
        "#Seg.Complex#" => Seg;
        "#Seg.Complex#" = {
            Ap > Am;
        }
    }

    [flow] F = {        // GVT.Flow
        C1, C2 > C3, C4 |> C5;
C3 > C5 > C6;
C4 > C5;
        Main        // GVT.{ Segment | Parenting }
        > R3        // GVT.{ Segment }
        ;
        Main = {        // GVT.{ Segment | Parenting }
            // diamond
            Ap1 > Am1 > Bm1;
            Ap1 > Bp1 > Bm1;

            // diamond 2nd
            Bm1 >               // GVT.{ Child | Call | Aliased }
            Ap2 > Am2 > Bm2;
            Ap2 > Bp2 > Bm2;

            Ap>Am>Bp>Bm;
            Bm2
            > Ap             // GVT.{ Child | Call }
            ;
        }
        R1              // define my local terminal real segment    // GVT.{ Segment }
            //> C."+"     // direct interface call wrapper segment    // GVT.{ Call }
            > Main2     // aliased to my real segment               // GVT.{ Segment | Aliased }
                        // aliased to interface                     // GVT.{ Segment | Aliased | Call }
            ;
        R2;

        [aliases] = {
            Main.Ap = { Ap1; Ap2; }
            Main.Am = { Am1; Am2; }
            Main.Bp = { Bp1; Bp2; }
            Main.Bm = { Bm1; Bm2; }
            Main = { Main2; }
        }
        // Flow 내의 safety 는 지원하지 않음
        //[safety] = {
        //    F.Main = { Ap; }
        //}
    }

    [jobs] = {
        Ap = { A."+"(%I1:APIN, %Q1); }
        Am = { A."-"(%I2, %Q2); }
        Bp = { B."+"(%I3, %Q3); }
        Bm = { B."-"(%I4, %Q4); }
    }
    [variables] = {
        Int32 D200;
        const Int32 D100 = 5;
    }
    [operators] = {
        STN1_ON = #{$APIN == true;}
    }
    [commands] = {
        STN1_COMMAD1 = #{$D100 = 560;}
    }

    [buttons] = {
        [e] = {
            EMGBTN(_,_) = { F; }
            //EmptyButton = {}
            //NonExistingFlowButton = { F1; }
        }
    }

    [prop] = {
        // safety : Real|Call = { (Real|Call)* }
        [safety] = {
            F.Main = { F.Main.Ap; }
            F.Main.Am = { F.Main; }
        }
        [autopre] = {
            F.Main.Am = { F.Main.Ap; }
        }
        [layouts file="cctv1;rtsp://210.99.70.120:1935/live/cctv002.stream"] = {
            A = (1309, 405, 205, 83);
            C = (1600, 500, 300, 300);
        }
        [layouts] = {
            C = (1600, 500, 300, 300);
        }
        [finish] = {
            F.R1;
            F.R2;
        }
        [disable] = {
            F.Main.Ap
        }
    }

    [device file="cylinder.ds"] A, B;
    [external file="station.ds"] C;
}
"""

    let CpuTestText =
        """
[sys] My = {
    [flow] MyFlow = {
        Ap > Seg1 > Seg2 > F.R3;
        Seg2 > aliasRealInFlow > aliasRealExInFlow;
        Seg1 = {
            Ap > Am > aliasCallInReal;
        }
        Seg2 = {
            Ap > Am;
        }

        [aliases] = {
            Seg1.Ap = { aliasCallInFlow; aliasCallInReal; }
            Seg1 = { aliasRealInFlow; }
            F.R3 = { aliasRealExInFlow; }
        }
    }

    [flow] F = {        // GVT.Flow
        C1, C2 > C3 > C4 |> C5;
C3 > C5 > C6;
C4 > C5;
        Main        // GVT.{ Segment | Parenting }
        > R3        // GVT.{ Segment }
        ;
        Main = {        // GVT.{ Segment | Parenting }
            // diamond
            Ap1 > Am1 > Bm1;
            Ap1 > Bp1 > Bm1;

            // diamond 2nd
            Bm1 >               // GVT.{ Child | Call | Aliased }
            Ap2 > Am2 > Bm2;
            Ap2 > Bp2 > Bm2;

            Ap>Am>Bp>Bm;
            Bm2
            > Ap             // GVT.{ Child | Call }
            ;
        }
        R1              // define my local terminal real segment    // GVT.{ Segment }
            //> C."+"     // direct interface call wrapper segment    // GVT.{ Call }
            > Main2     // aliased to my real segment               // GVT.{ Segment | Aliased }
            //> Ap1       // aliased to interface                     // GVT.{ Segment | Aliased | Call }
            ;
        R2;

        [aliases] = {
            Main.Ap = { Ap1; Ap2; }
            Main.Am = { Am1; Am2; }
            Main.Bp = { Bp1; Bp2; }
            Main.Bm = { Bm1; Bm2; }
            Main = { Main2; }
        }
        // Flow 내의 safety 는 지원하지 않음
        //[safety] = {
        //    F.Main = { Ap; }
        //}
    }

    [jobs] = {
        Ap = { A."+"(_, _); }
        Am = { A."-"(_, _); }
        Bp = { B."+"(_, _); }
        Bm = { B."-"(_, _); }
    }

    [buttons] = {
        [e] = {
            EMGBTN(_,_) = { F; }
            //EmptyButton = {}
            //NonExistingFlowButton = { F1; }
        }
    }

    [prop] = {
        // safety : Real|Call = { (Real|Call)* }
        [safety] = {
            F.Main = { F.Main.Ap; }
            F.Main.Am = { F.Main; }
        }
        [layouts] = {
            A = (1309,405,205,83);
        }
    }

    [device file="cylinder.ds"] A;
    [device file="cylinder.ds"] B;
    [external file="station.ds"] C;
}
"""

    let CodeElementsText =
        """
[sys] My = {
    [flow] F = {
        Seg1;
    }

    [variables] = { //이름 = (타입,초기값)
        R100 = (word, 0)
        R101 = (word, 0)
        R102 = (word, 5)
        R103 = (dword, 0)
        PI = (float, 3.1415)
    }


}

"""

    let DuplicatedEdgesText =
        """
[sys] B = {
    [flow] F = {
        Vp > Pp;
        Vp |> Pp;
    }
}
"""

    let DuplicatedCallsText =
        """
[sys] My = {
    [flow] F = {
        main = {
                Fp > Fm > Gm;
        }
    }
    [jobs] = {
        Fp = { F."+"(%I1, %Q1); }
        Fm = { F."-"(%I2, %Q2); }
        Gm = { G."-"(%I3, %Q3); }
    }
    [device file="cylinder.ds"] 
        F, 
        G;
}

"""

    let CausalsText =
        """
[sys] L = {
    [flow] F = {
        //Ap > Am;
        Main = {

           // Ap1 > Bp1;
            Ap > Am > Bp;

            /* Grouped */
            //{ Ap1; Bp1; } > Bm1
            //{ Ap1; Bp1; } > { Am1; Bm1; }
        }
        [aliases] = {
            Main.Bp = { Bp1; Bp2; Bp3; }
            //Bm = { Bm1; Bm2; Bm3; } Vextex에 없으면 정의불가
        }
    }
    [jobs] = {
        Ap = { A."+"(%I1, %Q1); }
        Am = { A."-"(%I2, %Q2); }
        Bp = { B."+"(%I3, %Q3); }
        Bm = { B."-"(%I4, %Q4); }
    }

    

    [device file="cylinder.ds"]
    A,
    B;
}
"""

    let AdoptoedValidText =
        """
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;
        Seg1 = {
            Ap > Am;
        }
    }
    [flow] F2 = {
        F.Seg1 > Seg;
        Seg = {
            Ap > Am;
        }
    }
    [jobs] = {
        Ap = { A."+"(%I1, %Q1); }
        Am = { A."-"(%I2, %Q2); }
    }

    [device file="cylinder.ds"] A;
}

"""

    let SimpleLoadedDeviceText =
        """
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;
        Seg1 = {
            Fp > Fm;
        }
    }
    [flow] F2 = {
        F.Seg1 > Seg;
        Seg = {
            Fp > Fm;
        }
    }
    [jobs] = {
        Fp = { F."+"(%I1, %Q1); }
        Fm = { F."-"(%I2, %Q2); }
    }
    [device file="cylinder.ds"] F;
}

"""

    let SplittedMRIEdgesText =
        """
[sys] A = {
    [flow] F = {
        a3 <|> a4;
        a1 <|> a2 |> a3 |> a2;
        a1 > a2 > a3 > a4;
    }
    [interfaces] = {
        I1 = { F.a1 ~ F.a2 }
        I2 = { F.a2 ~ F.a3 }
        I3 = { F.a3 ~ F.a1 }
        I1 <|> I2;
        I1 <|> I3;
        I1 <|> I4;
        I2 <|> I3;
        I2 <|> I4;
        I3 <|> I4;
    }
}
"""

    let PptGeneratedText =
        """
[sys] SIDE_QTR_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_QTR_Handling.LOADING1" = { _ ~ _ }
        "SIDE_QTR_Handling.LOADING2" = { _ ~ _ }
    }
}
[sys] Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] MY = {
    [flow] S101 = {
        "S101_Handling.LOADING1" > "S101_F_APRON_U131.ADV" > "S101_F_APRON_P132_134Unit.UP" > "S101_F_APRON.CLAMP" > "S101_Handling.LOADING2" > "S101_DASH_P112.UP" > "S101_DASH_P114.LATCH" > "S101_DASH.CLAMP" > "S101_Weld.WELDING" > "S101_DASH.UNCLAMP" > "S101_DASH_P114.UNLATCH" > "S101_DASH_U111_1.UNLATCH" > "S101_Handling.UNLOADING" > "S101_F_APRON_P104.UP";
        "S101_DASH_P114.UNLATCH" > "S101_DASH_U115.UNLATCH" > "S101_Handling.UNLOADING";
        "S101_DASH_P114.UNLATCH" > "S101_DASH_UNIT.RET" > "S101_Handling.UNLOADING";
        "S101_DASH.UNCLAMP" > "S101_F_APRON_P132_134Unit.DOWN" > "S101_DASH_U111_1.UNLATCH";
        "S101_F_APRON_P132_134Unit.DOWN" > "S101_DASH_U115.UNLATCH";
        "S101_F_APRON_P132_134Unit.DOWN" > "S101_DASH_UNIT.RET";
        "S101_DASH.UNCLAMP" > "S101_F_APRON_U131.RET" > "S101_DASH_U111_1.UNLATCH";
        "S101_F_APRON_U131.RET" > "S101_DASH_U115.UNLATCH";
        "S101_F_APRON_U131.RET" > "S101_DASH_UNIT.RET";
        "S101_Weld.WELDING" > "S101_DASH_P112.DOWN" > "S101_DASH_P114.UNLATCH";
        "S101_DASH_P112.DOWN" > "S101_F_APRON_P132_134Unit.DOWN";
        "S101_DASH_P112.DOWN" > "S101_F_APRON_U131.RET";
        "S101_Weld.WELDING" > "S101_F_APRON.UNCLAMP" > "S101_DASH_P114.UNLATCH";
        "S101_F_APRON.UNCLAMP" > "S101_F_APRON_P132_134Unit.DOWN";
        "S101_F_APRON.UNCLAMP" > "S101_F_APRON_U131.RET";
        "S101_Weld.WELDING" > "S101_F_APRON_P104.DOWN" > "S101_DASH_P114.UNLATCH";
        "S101_F_APRON_P104.DOWN" > "S101_F_APRON_P132_134Unit.DOWN";
        "S101_F_APRON_P104.DOWN" > "S101_F_APRON_U131.RET";
        "S101_Weld.WELDING" > "S101_F_APRON_U133.UNCLAMP" > "S101_DASH_P114.UNLATCH";
        "S101_F_APRON_U133.UNCLAMP" > "S101_F_APRON_P132_134Unit.DOWN";
        "S101_F_APRON_U133.UNCLAMP" > "S101_F_APRON_U131.RET";
        "S101_DASH_P114.LATCH" > "S101_DASH_U111_1.LATCH" > "S101_Weld.WELDING";
        "S101_Handling.LOADING2" > "S101_DASH_U115.LATCH" > "S101_DASH_P114.LATCH";
        "S101_Handling.LOADING2" > "S101_DASH_UNIT.ADV" > "S101_DASH_P114.LATCH";
        "S101_F_APRON_P132_134Unit.UP" > "S101_F_APRON_U133.CLAMP" > "S101_Handling.LOADING2";
    }
    [flow] SIDE_REINF = {
        "#201-1" = {
            SIDE_REINF_Handling."SIDE_REINF_Handling.LOADING1" > SIDE_REINF_REINF_Shift."SIDE_REINF_REINF_Shift.ADV" > SIDE_REINF_REINF_Pin."SIDE_REINF_REINF_Pin.UP" > SIDE_REINF_Handling."SIDE_REINF_Handling.LOADING2" > SIDE_REINF_REINF1_Clamp."SIDE_REINF_REINF1_Clamp.CLAMP" > SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF1_Clamp."SIDE_REINF_REINF1_Clamp.UNCLAMP";
            SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF2_Clamp."SIDE_REINF_REINF2_Clamp.UNCLAMP";
            SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF_Pin."SIDE_REINF_REINF_Pin.DOWN";
            SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING" > SIDE_REINF_REINF_Shift."SIDE_REINF_REINF_Shift.RET";
            SIDE_REINF_Handling."SIDE_REINF_Handling.LOADING2" > SIDE_REINF_REINF2_Clamp."SIDE_REINF_REINF2_Clamp.CLAMP" > SIDE_REINF_Weld."SIDE_REINF_Weld.WELDING";
        }
    }
    [flow] SIDE_QTR = {
        "SIDE_MAIN.#205" > "#205-1" > "#205-2";
        "#205-1" = {
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING1" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.ADV" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.UP" > SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF1_Clamp."SIDE_QTR_REINF1_Clamp.CLAMP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF1_Clamp."SIDE_QTR_REINF1_Clamp.UNCLAMP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF2_Clamp."SIDE_QTR_REINF2_Clamp.UNCLAMP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.DOWN";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.RET";
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF2_Clamp."SIDE_QTR_REINF2_Clamp.CLAMP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING";
        }
        "#205-2" = {
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING1" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.ADV" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.UP" > SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF1_Clamp."SIDE_QTR_REINF1_Clamp.CLAMP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF1_Clamp."SIDE_QTR_REINF1_Clamp.UNCLAMP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF2_Clamp."SIDE_QTR_REINF2_Clamp.UNCLAMP";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Pin."SIDE_QTR_REINF_Pin.DOWN";
            SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING" > SIDE_QTR_REINF_Shift."SIDE_QTR_REINF_Shift.RET";
            SIDE_QTR_Handling."SIDE_QTR_Handling.LOADING2" > SIDE_QTR_REINF2_Clamp."SIDE_QTR_REINF2_Clamp.CLAMP" > SIDE_QTR_Weld."SIDE_QTR_Weld.WELDING";
        }
    }
    [flow] SIDE_MAIN = {
        "SIDE_REINF.#201-1" > "#201" > "#202" > "#205" > "SIDE_QTR.#205-2" > "#206";
        "#201" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
        "#202" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
        "#206" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
        "#205" = {
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING1" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.ADV" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.UP" > SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF1_Clamp."SIDE_MAIN_REINF1_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.UNCLAMP";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Pin."SIDE_MAIN_REINF_Pin.DOWN";
            SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING" > SIDE_MAIN_REINF_Shift."SIDE_MAIN_REINF_Shift.RET";
            SIDE_MAIN_Handling."SIDE_MAIN_Handling.LOADING2" > SIDE_MAIN_REINF2_Clamp."SIDE_MAIN_REINF2_Clamp.CLAMP" > SIDE_MAIN_Weld."SIDE_MAIN_Weld.WELDING";
        }
    }
}
[sys] S101_F_APRON_P104 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] SIDE_MAIN_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_MAIN_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] S101_F_APRON_U133 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_REINF_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_MAIN_Handling.LOADING2" = { _ ~ _ }
        "SIDE_MAIN_Handling.LOADING1" = { _ ~ _ }
    }
}
[sys] S101_F_APRON_U131 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF2_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_REINF_REINF2_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] SIDE_REINF_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_REINF_Handling.LOADING2" = { _ ~ _ }
        "SIDE_REINF_Handling.LOADING1" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF1_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_MAIN_REINF1_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF1_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_QTR_REINF1_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] S101_DASH_P114 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] S101_DASH_U111_1 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_QTR_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_QTR_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] S101_F_APRON_P132_134Unit = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] Robot = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_MAIN_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF2_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_MAIN_REINF2_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] S101_DASH = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
}
[sys] S101_DASH_U115 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] S101_DASH_P112 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
}
[sys] S101_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_QTR_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_QTR_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] S101_DASH_UNIT = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_REINF_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_QTR_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] Latch = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <|> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <|> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_REINF_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_MAIN_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <|> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_MAIN_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_MAIN_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] S101_F_APRON = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
}
[sys] S101_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <|> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <|> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <|> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_REINF_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF1_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_REINF_REINF1_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_QTR_REINF2_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_QTR_REINF2_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <|> "UNCLAMP [Vm ~ Sm]";
    }
}
"""

    let RecursiveSystemText =
        """
[sys] P = {
    [sys] P1 = {
        [flow] F = {
            Vp > Vm;
        }
    }

    [sys] P2 = {
        [flow] F = {
            Vp > Vm;
        }
    }
}
"""

    let Ppt20221213Text =
        """
[sys] FactoryIO = {
    [flow] API = {
    }
    [flow] Line = {
        Clear |> Clear |> Copy1_Assy반출 |> Green공급및고정;		// Clear(Real)|> Clear(Real) |> Copy1_Assy반출(Alias) |> Green공급및고정(Real);
        "Line_Robot_X+" > 로봇조립Sub;		// "Line_Robot_X+"(Call)> 로봇조립Sub(Real);
        "Line_Robot_X-" > 로봇조립Main;		// "Line_Robot_X-"(Call)> 로봇조립Main(Real);
        Blue공급및고정 => 로봇조립Sub => Assy반출;		// Blue공급및고정(Real)=> 로봇조립Sub(Real) => Assy반출(Real);
        GoSub => Blue공급및고정 => Assy반출;		// GoSub(Real)=> Blue공급및고정(Real) => Assy반출(Real);
        GoMain => Green공급및고정 => 로봇조립Main => 로봇조립Sub;		// GoMain(Real)=> Green공급및고정(Real) => 로봇조립Main(Real) => 로봇조립Sub(Real);
        Assy반출 = {
            "Line_Assy_Upper+" > Line_Sub_PartOff;		// "Line_Assy_Upper+"(Call)> Line_Sub_PartOff(Call);
            "Line_Assy_Upper+" > "Line_Assy_UpConv+" > "Line_Assy_Upper-" > "Line_Assy_DownConv+";		// "Line_Assy_Upper+"(Call)> "Line_Assy_UpConv+"(Call) > "Line_Assy_Upper-"(Call) > "Line_Assy_DownConv+"(Call);
        }
        Green공급및고정 = {
            "Line_Main_Conv+" > "Line_Main_Clamp+" > "Line_Main_Clamp-";		// "Line_Main_Conv+"(Call)> "Line_Main_Clamp+"(Call) > "Line_Main_Clamp-"(Call);
        }
        Blue공급및고정 = {
            "Line_Sub_Conv+" > "Line_Sub_Clamp+" > "Line_Sub_Clamp-";		// "Line_Sub_Conv+"(Call)> "Line_Sub_Clamp+"(Call) > "Line_Sub_Clamp-"(Call);
        }
        로봇조립Sub = {
            "Line_Robot_Grab-" > "Line_Robot_Z-";		// "Line_Robot_Grab-"(Call)> "Line_Robot_Z-"(Call);
            "Line_Robot_Z+" > "Line_Robot_Grab-" > "Line_Robot_X-";		// "Line_Robot_Z+"(Call)> "Line_Robot_Grab-"(Call) > "Line_Robot_X-"(Call);
        }
        로봇조립Main = {
            "Line_Robot_Grab+" > Line_Main_PartOff;		// "Line_Robot_Grab+"(Call)> Line_Main_PartOff(Call);
            "Line_Robot_Grab+" > "Line_Robot_X+";		// "Line_Robot_Grab+"(Call)> "Line_Robot_X+"(Call);
            "Line_Robot_Z+" > "Line_Robot_Grab+" > "Line_Robot_Z-";		// "Line_Robot_Z+"(Call)> "Line_Robot_Grab+"(Call) > "Line_Robot_Z-"(Call);
        }
        [aliases] = {
            Assy반출 = { Copy1_Assy반출; }
        }
    }
    [jobs] = {
        "Line_Sub_Conv+" = { Line_Sub."Conv+"(_, _); }
        Line_Sub_PartOff = { Line_Sub.PartOff(_, _); }
        "Line_Sub_Clamp+" = { Line_Sub."Clamp+"(_, _); }
        "Line_Sub_Clamp-" = { Line_Sub."Clamp-"(_, _); }
        "Line_Robot_Grab+" = { Line_Robot."Grab+"(_, _); }
        "Line_Robot_Grab-" = { Line_Robot."Grab-"(_, _); }
        "Line_Robot_X-" = { Line_Robot."X-"(_, _); }
        "Line_Robot_Z+" = { Line_Robot."Z+"(_, _); }
        "Line_Robot_Z-" = { Line_Robot."Z-"(_, _); }
        "Line_Robot_X+" = { Line_Robot."X+"(_, _); }
        "Line_Assy_UpConv+" = { Line_Assy."UpConv+"(_, _); }
        "Line_Assy_DownConv+" = { Line_Assy."DownConv+"(_, _); }
        "Line_Assy_Upper+" = { Line_Assy."Upper+"(_, _); }
        "Line_Assy_Upper-" = { Line_Assy."Upper-"(_, _); }
        Line_Assy_JobClear = { Line_Assy.JobClear(_, _); }
        "Line_Main_Conv+" = { Line_Main."Conv+"(_, _); }
        Line_Main_PartOff = { Line_Main.PartOff(_, _); }
        "Line_Main_Clamp+" = { Line_Main."Clamp+"(_, _); }
        "Line_Main_Clamp-" = { Line_Main."Clamp-"(_, _); }
    }
    [interfaces] = {
        "JobClearClear~_" = { _ ~ _ }
        "StartMainGoMain~_" = { _ ~ _ }
        "StartSubGoSub~_" = { _ ~ _ }
    }
    [device file="Lib/Sub.ds"] Line_Sub; // C:\Users\kwak\Downloads\FactoryIO\Lib\Sub.pptx
    [device file="Lib/Robot.ds"] Line_Robot; // C:\Users\kwak\Downloads\FactoryIO\Lib\Robot.pptx
    [device file="Lib/Assy.ds"] Line_Assy; // C:\Users\kwak\Downloads\FactoryIO\Lib\Assy.pptx
    [device file="Lib/Main.ds"] Line_Main; // C:\Users\kwak\Downloads\FactoryIO\Lib\Main.pptx
}
"""


    let ParseNormal (text: string) =
        let systemRepo = ShareableSystemRepository()

        ModelParser.ParseFromString2(
            text,
            ParserOptions.Create4Simulation(systemRepo, ".", "ActiveCpuName", None, DuNone)
        )
        |> ignore

        debugfn "Done"


    let Main (_args: string[]) =
        //ParseNormal(SplittedMRIEdgesText)
        //ParseNormal(DuplicatedEdgesText)
        //ParseNormal(AdoptoedValidText)
        //ParseNormal(AdoptoedAmbiguousText)
        //ParseNormal(CodeElementsText)
        ParseNormal(EveryScenarioText)
    //ParseNormal(PptGeneratedText)

    let ReadAllInput (fn: string) = System.IO.File.ReadAllText(fn)
