namespace Engine.Parser.FS

open Antlr4.Runtime

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser

module Program =

    let EveryScenarioText = """
[sys ip = 192.168.0.1] My = {
    [flow] MyFlow = {
        Seg1 > Seg2 > Ap;
        Seg1 = {
            Ap > Am;
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

            Bm2
            > Ap             // GVT.{ Child | Call }
            ;
        }
        R1              // define my local terminal real segment    // GVT.{ Segment }
            //> C."+"     // direct interface call wrapper segment    // GVT.{ Call }
            > Main2     // aliased to my real segment               // GVT.{ Segment | Aliased }
            > Ap1       // aliased to interface                     // GVT.{ Segment | Aliased | Call }
            ;
        R2;

        [aliases] = {
            Ap = { Ap1; Ap2; }
            Am = { Am1; Am2; }
            Bp = { Bp1; Bp2; }
            Bm = { Bm1; Bm2; }
            Main = { Main2; }
        }
        // Flow 내의 safety 는 지원하지 않음
        //[safety] = {
        //    Main = { F.Main.Ap1; F.R2; }
        //}
    }

    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
        Bp = { B."+"(%Q3, %I3); }
        Bm = { B."-"(%Q4, %I4); }
    }

    [device file="cylinder.ds"] A;
    [device file="cylinder.ds"] B;
    [external file="station.ds" ip="192.168.0.2"] C;
    [prop] = {
        // Global safety
        [safety] = {
            F.Main = { F.Main.Ap1; F.R2 }
        }
        [layouts] = {
            Ap = (1309,405,205,83)
        }
    }

    [emg] = {
        EMGBTN = { F; };
        //EmptyButton = {};
        //NonExistingFlowButton = { F1; };
    }
}
"""

    let CodeElementsText = """
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

    [commands] = {
        CMD1 = (@Delay = 0)
        CMD2 = (@Delay = 30)
        CMD3 = (@add = 30, 50 ~ R103)  //30+R101 = R103
    }

    [observes] = {
        CON1 = (@GT = R102, 5)
        CON2 = (@Delay = 30)
        CON3 = (@Not = Tag1)
    }
}

"""
    let DuplicatedEdgesText = """
[sys] B = {
    [flow] F = {
        Vp > Pp;
        Vp |> Pp;
    }
}
"""

    let DuplicatedCallsText = """
[sys] My = {
    [flow] F = {
        Fp > Fm;
        Fm > Gm;
    }
    [calls] = {
        Fp = {F."+"(%Q1, %I1); }
        Fm = {F."-"(%Q2, %I2); }
        Gm = {G."-"(%Q3, %I3); }
    }
    [device file="cylinder.ds"] F;
    [device file="cylinder.ds"] G;
}

"""


    let AdoptoedValidText = """
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;
        Seg1 = {
            A."+" > A."-";
        }
    }
    [flow] F2 = {
        F.Seg1 > Seg;
        Seg = {
            A."+" > A."-";
        }
    }

    [device file="cylinder.ds"] A;
}

"""
    let AdoptoedAmbiguousText = """
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
    [calls] = {
        Fp = {F."+"(%Q1, %I1); }
        Fm = {F."-"(%Q2, %I2); }
    }
    [device file="cylinder.ds"] F;
}

"""
    let SplittedMRIEdgesText = """
[sys] A = {
    [flow] F = {
        //a1 <||> a2 <||> a3 <||> a4;

        a1 <||> a2;
        a2 ||> a3;  a3 ||> a2;
        a3 <||> a4;

        a1 > a2 > a3 > a4;
    }
    [interfaces] = {
        I1 = { F.a1 ~ F.a2 }
        I2 = { F.a2 ~ F.a3 }
        I3 = { F.a3 ~ F.a1 }
        // 정보로서의 상호 리셋
        I1 <||> I2 <||> I3 ||> I4;
    }
}
"""

    let PptGeneratedText = """
[sys] SIDE_QTR_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
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
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
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
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
    }
}
[sys] SIDE_MAIN_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
    [interfaces] = {
        "SIDE_REINF_Weld.WELDING" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
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
        "ADV [Vp ~ Sp]" <||> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF2_Clamp.CLAMP" = { _ ~ _ }
        "SIDE_REINF_REINF2_Clamp.UNCLAMP" = { _ ~ _ }
    }
}
[sys] SIDE_REINF_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
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
        "LATCH[Vp ~ Sp]" <||> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] S101_DASH_U111_1 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <||> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <||> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_QTR_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <||> "RET[Vm ~ Sm]";
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
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
    }
}
[sys] Robot = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_MAIN_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
    }
}
[sys] S101_DASH_U115 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "LATCH[Vp ~ Sp]" <||> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] S101_DASH_P112 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
    }
}
[sys] S101_Handling = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_QTR_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
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
        "ADV [Vp ~ Sp]" <||> "RET[Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        "ADV [Vp ~ Sp]" <||> "RET[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF_Shift.ADV" = { _ ~ _ }
        "SIDE_REINF_REINF_Shift.RET" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
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
        "LATCH[Vp ~ Sp]" <||> "UNLATCH [Vm ~ Sm]";
    }
}
[sys] SIDE_REINF_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "UP [Vp ~ Sp]" <||> "DOWN[Vm ~ Sm]";
    }
    [interfaces] = {
        "SIDE_REINF_REINF_Pin.UP" = { _ ~ _ }
        "SIDE_REINF_REINF_Pin.DOWN" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
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
        "ADV [Vp ~ Sp]" <||> "RET[Vm ~ Sm]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
    }
}
[sys] S101_Weld = {
    [flow] exflow = {
        "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]" <||> "WELDING [Robot_진입OK3 ~ Robot_작업완료3]";
        "LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]" <||> "LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]";
        "WELDING [Robot_진입OK3 ~ Robot_작업완료3]" <||> "UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]";
    }
}
[sys] SIDE_REINF_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
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
        "CLAMP [Vp ~ Sp]" <||> "UNCLAMP [Vm ~ Sm]";
    }
}
"""

    let RecursiveSystemText = """
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


    let ParseNormal(text:string) =
        let helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(".", "ActiveCpuName"))
        let system = helper.TheSystem.Value

        tracefn "---- Spit result"
        let spits = system.Spit()
        for spit in spits do
            match spit.SpitObj with
            | SpitFlow f -> f.Graph.Dump() |> ignore
            | SpitVertexReal r -> r.Graph.Dump() |> ignore
            | _ -> ()


        tracefn "Done"


    let Main(args:string[]) =
        //ParseNormal(SplittedMRIEdgesText)
        //ParseNormal(DuplicatedEdgesText)
        //ParseNormal(AdoptoedValidText)
        //ParseNormal(AdoptoedAmbiguousText)
        //ParseNormal(CodeElementsText)
        ParseNormal(EveryScenarioText)
        //ParseNormal(PptGeneratedText)

    let Try(input:string) =
        let str = new AntlrInputStream(input)
        System.Console.WriteLine(input)
        let lexer = new dsLexer(str)
        let tokens = new CommonTokenStream(lexer)
        let parser = new dsParser(tokens)
        //let listener_lexer = new ErrorListener<int>()
        //let listener_parser = new ErrorListener<IToken>()
        //lexer.AddErrorListener(listener_lexer)
        //parser.AddErrorListener(listener_parser)
        //let tree = parser.file()
        //if (listener_lexer.had_error || listener_parser.had_error)
        //    System.Console.WriteLine("error in parse.")
        //else
        //    System.Console.WriteLine("parse completed.")
        ()


    let ReadAllInput(fn:string) = System.IO.File.ReadAllText(fn)
