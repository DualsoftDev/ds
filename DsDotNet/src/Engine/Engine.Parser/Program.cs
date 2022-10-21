// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser;

public class Program
{
    static string EveryScenarioText = @"
[sys ip = 192.168.0.1] My = {
    [flow] MyFlow = {
        Seg1 > Seg2;
        Seg1 = {
            A.""+"" > A.""-"";
        }
    }
    [flow] ""Flow.Complex"" = {
        ""#Seg.Complex#"" > Seg;
        ""#Seg.Complex#"" = {
            A.""+"" > A.""-"";
        }
    }

    [flow] F = {        // GraphVertexType.Flow
        C1, C2 > C3, C4 |> C5;
C3 > C5 > C6;
C4 > C5;
        Main        // GraphVertexType.{ Segment | Parenting }
        > R3        // GraphVertexType.{ Segment }
        ;
        Main = {        // GraphVertexType.{ Segment | Parenting }
            // diamond
            Ap1 > Am1 > Bm1;
            Ap1 > Bp1 > Bm1;

            // diamond 2nd
            Bm1 >               // GraphVertexType.{ Child | Call | Aliased }
            Ap2 > Am2 > Bm2;
            Ap2 > Bp2 > Bm2;

            Bm2
            > A.""+""             // GraphVertexType.{ Child | Call }
            ;
        }
        R1              // define my local terminal real segment    // GraphVertexType.{ Segment }
            //> C.""+""     // direct interface call wrapper segment    // GraphVertexType.{ Call }
            > Main2     // aliased to my real segment               // GraphVertexType.{ Segment | Aliased }
            > Ap1       // aliased to interface                     // GraphVertexType.{ Segment | Aliased | Call }
            ;
        R2;

        [aliases] = {
            A.""+"" = { Ap1; Ap2; }
            A.""-"" = { Am1; Am2; }
            B.""+"" = { Bp1; Bp2; }
            B.""-"" = { Bm1; Bm2; }
            Main = { Main2; }
        }
        [safety] = {
            Main = {A.F.Sp; A.F.Sm}
        }
    }
    [emg] = {
        EMGBTN = { F; };
        //EmptyButton = {};
        //NonExistingFlowButton = { F1; };
    }
}
[sys ip=1.2.3.4] A = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <||> Vm;
    }
    [interfaces] = {
        ""+"" = { F.Vp ~ F.Sp }
        ""-"" = { F.Vm ~ F.Sm }
        // 정보로서의 상호 리셋
        ""+"" <||> ""-"";
    }
}
[sys] B = @copy_system(A);
[sys] C = @copy_system(A);
[prop] = {
    // Global safety
    [safety] = {
        My.F.Main = {B.F.Sp; B.F.Sm; C.F.Sp}
    }
    [addresses] = {
        A.""+"" = (%Q1234.2343, %I1234.2343)
    }
    [layouts] = {
        A.""+"" = (1309,405,205,83)
    }
}
";
    static string DuplicatedEdgesText = @"
[sys] B = {
    [flow] F = {
        Vp > Pp;
        Vp |> Pp;
    }
}
";

    static string AdoptoedText = @"
[sys] My = {
    [flow] F = {
        Seg1 > Seg2;
        Seg1 = {
            F.""+"" > F.""-"";
        }
    }
    [flow] F2 = {
        F.Seg1 > Seg;
        Seg = {
            F.""+"" > F.""-"";
        }
    }
}

[sys] F = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <||> Vm;
    }
    [interfaces] = {
        ""+"" = { F.Vp ~ F.Sp }
        ""-"" = { F.Vm ~ F.Sm }
        Seg1 = { F.Vp ~ F.Sp }
        // 정보로서의 상호 리셋
        ""+"" <||> ""-"";
    }
}
";

    static string SplittedMRIEdgesText = @"
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
";

    static string PptGeneratedText = @"
[sys] SIDE_QTR_Handling = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
    [interfaces] = {
        ""SIDE_QTR_Handling.LOADING1"" = { _ ~ _ }
        ""SIDE_QTR_Handling.LOADING2"" = { _ ~ _ }
    }
}
[sys] Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
}
[sys] MY = {
    [flow] S101 = {
        ""S101_Handling.LOADING1"" > ""S101_F_APRON_U131.ADV"" > ""S101_F_APRON_P132_134Unit.UP"" > ""S101_F_APRON.CLAMP"" > ""S101_Handling.LOADING2"" > ""S101_DASH_P112.UP"" > ""S101_DASH_P114.LATCH"" > ""S101_DASH.CLAMP"" > ""S101_Weld.WELDING"" > ""S101_DASH.UNCLAMP"" > ""S101_DASH_P114.UNLATCH"" > ""S101_DASH_U111_1.UNLATCH"" > ""S101_Handling.UNLOADING"" > ""S101_F_APRON_P104.UP"";
        ""S101_DASH_P114.UNLATCH"" > ""S101_DASH_U115.UNLATCH"" > ""S101_Handling.UNLOADING"";
        ""S101_DASH_P114.UNLATCH"" > ""S101_DASH_UNIT.RET"" > ""S101_Handling.UNLOADING"";
        ""S101_DASH.UNCLAMP"" > ""S101_F_APRON_P132_134Unit.DOWN"" > ""S101_DASH_U111_1.UNLATCH"";
        ""S101_F_APRON_P132_134Unit.DOWN"" > ""S101_DASH_U115.UNLATCH"";
        ""S101_F_APRON_P132_134Unit.DOWN"" > ""S101_DASH_UNIT.RET"";
        ""S101_DASH.UNCLAMP"" > ""S101_F_APRON_U131.RET"" > ""S101_DASH_U111_1.UNLATCH"";
        ""S101_F_APRON_U131.RET"" > ""S101_DASH_U115.UNLATCH"";
        ""S101_F_APRON_U131.RET"" > ""S101_DASH_UNIT.RET"";
        ""S101_Weld.WELDING"" > ""S101_DASH_P112.DOWN"" > ""S101_DASH_P114.UNLATCH"";
        ""S101_DASH_P112.DOWN"" > ""S101_F_APRON_P132_134Unit.DOWN"";
        ""S101_DASH_P112.DOWN"" > ""S101_F_APRON_U131.RET"";
        ""S101_Weld.WELDING"" > ""S101_F_APRON.UNCLAMP"" > ""S101_DASH_P114.UNLATCH"";
        ""S101_F_APRON.UNCLAMP"" > ""S101_F_APRON_P132_134Unit.DOWN"";
        ""S101_F_APRON.UNCLAMP"" > ""S101_F_APRON_U131.RET"";
        ""S101_Weld.WELDING"" > ""S101_F_APRON_P104.DOWN"" > ""S101_DASH_P114.UNLATCH"";
        ""S101_F_APRON_P104.DOWN"" > ""S101_F_APRON_P132_134Unit.DOWN"";
        ""S101_F_APRON_P104.DOWN"" > ""S101_F_APRON_U131.RET"";
        ""S101_Weld.WELDING"" > ""S101_F_APRON_U133.UNCLAMP"" > ""S101_DASH_P114.UNLATCH"";
        ""S101_F_APRON_U133.UNCLAMP"" > ""S101_F_APRON_P132_134Unit.DOWN"";
        ""S101_F_APRON_U133.UNCLAMP"" > ""S101_F_APRON_U131.RET"";
        ""S101_DASH_P114.LATCH"" > ""S101_DASH_U111_1.LATCH"" > ""S101_Weld.WELDING"";
        ""S101_Handling.LOADING2"" > ""S101_DASH_U115.LATCH"" > ""S101_DASH_P114.LATCH"";
        ""S101_Handling.LOADING2"" > ""S101_DASH_UNIT.ADV"" > ""S101_DASH_P114.LATCH"";
        ""S101_F_APRON_P132_134Unit.UP"" > ""S101_F_APRON_U133.CLAMP"" > ""S101_Handling.LOADING2"";
    }
    [flow] SIDE_REINF = {
        ""#201-1"" = {
            SIDE_REINF_Handling.""SIDE_REINF_Handling.LOADING1"" > SIDE_REINF_REINF_Shift.""SIDE_REINF_REINF_Shift.ADV"" > SIDE_REINF_REINF_Pin.""SIDE_REINF_REINF_Pin.UP"" > SIDE_REINF_Handling.""SIDE_REINF_Handling.LOADING2"" > SIDE_REINF_REINF1_Clamp.""SIDE_REINF_REINF1_Clamp.CLAMP"" > SIDE_REINF_Weld.""SIDE_REINF_Weld.WELDING"" > SIDE_REINF_REINF1_Clamp.""SIDE_REINF_REINF1_Clamp.UNCLAMP"";
            SIDE_REINF_Weld.""SIDE_REINF_Weld.WELDING"" > SIDE_REINF_REINF2_Clamp.""SIDE_REINF_REINF2_Clamp.UNCLAMP"";
            SIDE_REINF_Weld.""SIDE_REINF_Weld.WELDING"" > SIDE_REINF_REINF_Pin.""SIDE_REINF_REINF_Pin.DOWN"";
            SIDE_REINF_Weld.""SIDE_REINF_Weld.WELDING"" > SIDE_REINF_REINF_Shift.""SIDE_REINF_REINF_Shift.RET"";
            SIDE_REINF_Handling.""SIDE_REINF_Handling.LOADING2"" > SIDE_REINF_REINF2_Clamp.""SIDE_REINF_REINF2_Clamp.CLAMP"" > SIDE_REINF_Weld.""SIDE_REINF_Weld.WELDING"";
        }
    }
    [flow] SIDE_QTR = {
        ""SIDE_MAIN.#205"" > ""#205-1"" > ""#205-2"";
        ""#205-1"" = {
            SIDE_QTR_Handling.""SIDE_QTR_Handling.LOADING1"" > SIDE_QTR_REINF_Shift.""SIDE_QTR_REINF_Shift.ADV"" > SIDE_QTR_REINF_Pin.""SIDE_QTR_REINF_Pin.UP"" > SIDE_QTR_Handling.""SIDE_QTR_Handling.LOADING2"" > SIDE_QTR_REINF1_Clamp.""SIDE_QTR_REINF1_Clamp.CLAMP"" > SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF1_Clamp.""SIDE_QTR_REINF1_Clamp.UNCLAMP"";
            SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF2_Clamp.""SIDE_QTR_REINF2_Clamp.UNCLAMP"";
            SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF_Pin.""SIDE_QTR_REINF_Pin.DOWN"";
            SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF_Shift.""SIDE_QTR_REINF_Shift.RET"";
            SIDE_QTR_Handling.""SIDE_QTR_Handling.LOADING2"" > SIDE_QTR_REINF2_Clamp.""SIDE_QTR_REINF2_Clamp.CLAMP"" > SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"";
        }
        ""#205-2"" = {
            SIDE_QTR_Handling.""SIDE_QTR_Handling.LOADING1"" > SIDE_QTR_REINF_Shift.""SIDE_QTR_REINF_Shift.ADV"" > SIDE_QTR_REINF_Pin.""SIDE_QTR_REINF_Pin.UP"" > SIDE_QTR_Handling.""SIDE_QTR_Handling.LOADING2"" > SIDE_QTR_REINF1_Clamp.""SIDE_QTR_REINF1_Clamp.CLAMP"" > SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF1_Clamp.""SIDE_QTR_REINF1_Clamp.UNCLAMP"";
            SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF2_Clamp.""SIDE_QTR_REINF2_Clamp.UNCLAMP"";
            SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF_Pin.""SIDE_QTR_REINF_Pin.DOWN"";
            SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"" > SIDE_QTR_REINF_Shift.""SIDE_QTR_REINF_Shift.RET"";
            SIDE_QTR_Handling.""SIDE_QTR_Handling.LOADING2"" > SIDE_QTR_REINF2_Clamp.""SIDE_QTR_REINF2_Clamp.CLAMP"" > SIDE_QTR_Weld.""SIDE_QTR_Weld.WELDING"";
        }
    }
    [flow] SIDE_MAIN = {
        ""SIDE_REINF.#201-1"" > ""#201"" > ""#202"" > ""#205"" > ""SIDE_QTR.#205-2"" > ""#206"";
        ""#201"" = {
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING1"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.ADV"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.UP"" > SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.DOWN"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.RET"";
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"";
        }
        ""#202"" = {
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING1"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.ADV"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.UP"" > SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.DOWN"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.RET"";
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"";
        }
        ""#206"" = {
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING1"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.ADV"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.UP"" > SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.DOWN"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.RET"";
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"";
        }
        ""#205"" = {
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING1"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.ADV"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.UP"" > SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF1_Clamp.""SIDE_MAIN_REINF1_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.UNCLAMP"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Pin.""SIDE_MAIN_REINF_Pin.DOWN"";
            SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"" > SIDE_MAIN_REINF_Shift.""SIDE_MAIN_REINF_Shift.RET"";
            SIDE_MAIN_Handling.""SIDE_MAIN_Handling.LOADING2"" > SIDE_MAIN_REINF2_Clamp.""SIDE_MAIN_REINF2_Clamp.CLAMP"" > SIDE_MAIN_Weld.""SIDE_MAIN_Weld.WELDING"";
        }
    }
}
[sys] S101_F_APRON_P104 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
}
[sys] SIDE_MAIN_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_MAIN_REINF_Pin.UP"" = { _ ~ _ }
        ""SIDE_MAIN_REINF_Pin.DOWN"" = { _ ~ _ }
    }
}
[sys] S101_F_APRON_U133 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
}
[sys] SIDE_REINF_Weld = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
    [interfaces] = {
        ""SIDE_REINF_Weld.WELDING"" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Handling = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
    [interfaces] = {
        ""SIDE_MAIN_Handling.LOADING2"" = { _ ~ _ }
        ""SIDE_MAIN_Handling.LOADING1"" = { _ ~ _ }
    }
}
[sys] S101_F_APRON_U131 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""ADV [Vp ~ Sp]"" <||> ""RET[Vm ~ Sm]"";
    }
}
[sys] SIDE_REINF_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_REINF_REINF2_Clamp.CLAMP"" = { _ ~ _ }
        ""SIDE_REINF_REINF2_Clamp.UNCLAMP"" = { _ ~ _ }
    }
}
[sys] SIDE_REINF_Handling = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
    [interfaces] = {
        ""SIDE_REINF_Handling.LOADING2"" = { _ ~ _ }
        ""SIDE_REINF_Handling.LOADING1"" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_MAIN_REINF1_Clamp.CLAMP"" = { _ ~ _ }
        ""SIDE_MAIN_REINF1_Clamp.UNCLAMP"" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_QTR_REINF1_Clamp.CLAMP"" = { _ ~ _ }
        ""SIDE_QTR_REINF1_Clamp.UNCLAMP"" = { _ ~ _ }
    }
}
[sys] S101_DASH_P114 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""LATCH[Vp ~ Sp]"" <||> ""UNLATCH [Vm ~ Sm]"";
    }
}
[sys] S101_DASH_U111_1 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""LATCH[Vp ~ Sp]"" <||> ""UNLATCH [Vm ~ Sm]"";
    }
}
[sys] Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""ADV [Vp ~ Sp]"" <||> ""RET[Vm ~ Sm]"";
    }
}
[sys] SIDE_QTR_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""ADV [Vp ~ Sp]"" <||> ""RET[Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_QTR_REINF_Shift.ADV"" = { _ ~ _ }
        ""SIDE_QTR_REINF_Shift.RET"" = { _ ~ _ }
    }
}
[sys] S101_F_APRON_P132_134Unit = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
}
[sys] Robot = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
}
[sys] SIDE_MAIN_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_MAIN_REINF2_Clamp.CLAMP"" = { _ ~ _ }
        ""SIDE_MAIN_REINF2_Clamp.UNCLAMP"" = { _ ~ _ }
    }
}
[sys] S101_DASH = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
}
[sys] S101_DASH_U115 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""LATCH[Vp ~ Sp]"" <||> ""UNLATCH [Vm ~ Sm]"";
    }
}
[sys] S101_DASH_P112 = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
}
[sys] S101_Handling = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
}
[sys] SIDE_QTR_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_QTR_REINF_Pin.UP"" = { _ ~ _ }
        ""SIDE_QTR_REINF_Pin.DOWN"" = { _ ~ _ }
    }
}
[sys] S101_DASH_UNIT = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""ADV [Vp ~ Sp]"" <||> ""RET[Vm ~ Sm]"";
    }
}
[sys] SIDE_REINF_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""ADV [Vp ~ Sp]"" <||> ""RET[Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_REINF_REINF_Shift.ADV"" = { _ ~ _ }
        ""SIDE_REINF_REINF_Shift.RET"" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_Weld = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
    [interfaces] = {
        ""SIDE_QTR_Weld.WELDING"" = { _ ~ _ }
    }
}
[sys] Latch = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""LATCH[Vp ~ Sp]"" <||> ""UNLATCH [Vm ~ Sm]"";
    }
}
[sys] SIDE_REINF_REINF_Pin = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""UP [Vp ~ Sp]"" <||> ""DOWN[Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_REINF_REINF_Pin.UP"" = { _ ~ _ }
        ""SIDE_REINF_REINF_Pin.DOWN"" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_Weld = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
    [interfaces] = {
        ""SIDE_MAIN_Weld.WELDING"" = { _ ~ _ }
    }
}
[sys] SIDE_MAIN_REINF_Shift = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vm |> Sp;
        Vp |> Sm;
        ""ADV [Vp ~ Sp]"" <||> ""RET[Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_MAIN_REINF_Shift.ADV"" = { _ ~ _ }
        ""SIDE_MAIN_REINF_Shift.RET"" = { _ ~ _ }
    }
}
[sys] S101_F_APRON = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
}
[sys] S101_Weld = {
    [flow] exflow = {
        ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"" <||> ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"";
        ""LOADING1 [Robot_진입OK1 ~ Robot_작업완료1]"" <||> ""LOADING2 [Robot_진입OK2 ~ Robot_작업완료2]"";
        ""WELDING [Robot_진입OK3 ~ Robot_작업완료3]"" <||> ""UNLOADING [Robot_진입OK4 ~ Robot_작업완료4]"";
    }
}
[sys] SIDE_REINF_REINF1_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_REINF_REINF1_Clamp.CLAMP"" = { _ ~ _ }
        ""SIDE_REINF_REINF1_Clamp.UNCLAMP"" = { _ ~ _ }
    }
}
[sys] SIDE_QTR_REINF2_Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
    [interfaces] = {
        ""SIDE_QTR_REINF2_Clamp.CLAMP"" = { _ ~ _ }
        ""SIDE_QTR_REINF2_Clamp.UNCLAMP"" = { _ ~ _ }
    }
}
[sys] Clamp = {
    [flow] exflow = {
        Vm > Pm > Sm;
        Vp > Pp > Sp;
        Vp |> Sm;
        Vm |> Sp;
        ""CLAMP [Vp ~ Sp]"" <||> ""UNCLAMP [Vm ~ Sm]"";
    }
}";



    static void ParseNormal(string text)
    {
        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation());
        var model = helper.Model;

        var xxx = model.ToDsText();
        //Try("1 + 2 + 3");
        //Try("1 2 + 3");
        //Try("1 + +");
        foreach (var kv in helper._elements)
        {
            var (p, type_) = (kv.Key, kv.Value);
            var types = type_.ToString("F");
            Trace.WriteLine(p.Combine("/")+$":{types}");
        }

        Trace.WriteLine("---- Spit result");
        var spits = model.Spit();
        foreach(var spit in spits)
        {
            var tName = spit.Obj.GetType().Name;
            var name = spit.NameComponents.Combine();
            Trace.WriteLine($"{name}:{tName}");
        }

        var spitObjs = spits.Select(spit => spit.Obj);
        var flowGraphs = spitObjs.OfType<Flow>().Select(f => f.Graph);
        var segGraphs = spitObjs.OfType<Real>().Select(s => s.Graph);
        foreach (var gr in flowGraphs)
            gr.Dump();
        foreach (var gr in segGraphs)
            gr.Dump();

        System.Console.WriteLine("Done");
    }


    public static void Main(string[] args)
    {
        //ParseNormal(SplittedMRIEdgesText);
        //ParseNormal(DuplicatedEdgesText);
        ParseNormal(AdoptoedText);
        ParseNormal(EveryScenarioText);
        ParseNormal(PptGeneratedText);
    }

    static void Try(string input)
    {
        var str = new AntlrInputStream(input);
        System.Console.WriteLine(input);
        var lexer = new dsLexer(str);
        var tokens = new CommonTokenStream(lexer);
        var parser = new dsParser(tokens);
        //var listener_lexer = new ErrorListener<int>();
        //var listener_parser = new ErrorListener<IToken>();
        //lexer.AddErrorListener(listener_lexer);
        //parser.AddErrorListener(listener_parser);
        //var tree = parser.file();
        //if (listener_lexer.had_error || listener_parser.had_error)
        //    System.Console.WriteLine("error in parse.");
        //else
        //    System.Console.WriteLine("parse completed.");
    }

    static string ReadAllInput(string fn)
    {
        var input = System.IO.File.ReadAllText(fn);
        return input;
    }
}
