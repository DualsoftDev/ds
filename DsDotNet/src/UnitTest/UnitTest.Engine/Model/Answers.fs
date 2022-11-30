namespace UnitTest.Engine

[<AutoOpen>]
module ModelAnswers =
    let answerCylinderText = """
[sys] Cylinder = {
    [flow] F = {
        Vp <||> Vm |> Pp |> Sm;
        Vp |> Pm |> Sp;
        Vm > Pm > Sm;
        Vp > Pp > Sp;
    }
    [interfaces] = {
        "+" = { F.Vp ~ F.Sp }
        "-" = { F.Vm ~ F.Sm }
        "+" <||> "-";
    }
}
"""
    let answerCausalsText = """
[sys] L = {
    [flow] F = {
        Ap > Am;
        Main = {

            Ap1 > Bp1;
            Ap > Am > Bp;

            /* Grouped */
            //{ Ap1; Bp1; } > Bm1
            //{ Ap1; Bp1; } > { Am1; Bm1; }
        }
        [aliases] = {
            Ap = { Ap1; Ap2; Ap3; }
            Am = { Am1; Am2; Am3; }
            Main.Bp = { Bp1; Bp2; Bp3; }
            //Bm = { Bm1; Bm2; Bm3; } Vextex에 없으면 정의불가
        }
    }
    [jobs] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
        Bp = { B."+"(%Q3, %I3); }
        Bm = { B."-"(%Q4, %I4); }
    }

    [prop] = {
        [safety] = {
            F.Main = { F.Ap; F.Am; }
            F.Ap = { F.Main; }
        }
    }

    [device file="cylinder.ds"] A;
    [device file="cylinder.ds"] B;
}

"""
    let answerEveryScenarioText = """
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
    [flow] F = {
        R1 > Main2 > Ap1;
        Main > R3;
        C4 > C5;
        C3 > C5 > C6;
        C1, C2 > C3, C4 |> C5;
        Main = {
            Ap2 > Bp2 > Bm2 > Ap;
            Ap1 > Bp1 > Bm1 > Ap2 > Am2 > Bm2;
            Ap1 > Am1 > Bm1;
        }
        R2; // island
        [aliases] = {
            Ap = { Ap1; Ap2; }
            Am = { Am1; Am2; }
            Bp = { Bp1; Bp2; }
            Bm = { Bm1; Bm2; }
            Main = { Main2; }
        }
    }
    [jobs] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
        Bp = { B."+"(%Q3, %I3); }
        Bm = { B."-"(%Q4, %I4); }
    }
    [emg] = {
        EMGBTN = { F; }
    }
    [prop] = {
        [safety] = {
            Am = { F.Main; }
            F.Main = { Ap; }
        }
        [layouts] = {
            Ap = (1309, 405, 205, 83)
        }
        // will not be supported
        //[addresses] = {
        //    A."+" = ( %Q1234.2343, %I1234.2343)
        //    A."-" = ( START, END)
        //    B."+" = ( %Q4321.2343, %I4321.2343)
        //    B."-" = ( BSTART, BEND)
        //}
    }
    [device file="cylinder.ds"] A;
    [device file="cylinder.ds"] B;
    [external file="station.ds"] C;
}

"""



    let answerSplittedMRIEdgesText = """
[sys] A = {
    [flow] F = {
        a3 <||> a4;
        a1 <||> a2 ||> a3 ||> a2;
        a1 > a2 > a3 > a4;
    }
    [interfaces] = {
        I1 = { F.a1 ~ F.a2 }
        I2 = { F.a2 ~ F.a3 }
        I3 = { F.a3 ~ F.a1 }
        I1 <||> I2;
        I2 <||> I3;
        I3 ||> I4;
    }
}
"""
    let answerDuplicatedEdgesText = """
[sys] B = {
    [flow] F = {
        Vp |> Pp;
        Vp > Pp;
    }
}
"""
    let answerDuplicatedCallsText = """
[sys] My = {
[flow] F = {
    Fp > Fm > Gm;
}
[jobs] = {
    Fp = { F."+"(%Q1, %I1); }
    Fm = { F."-"(%Q2, %I2); }
    Gm = { G."-"(%Q3, %I3); }
}
[device file="cylinder.ds"] F;
[device file="cylinder.ds"] G;
}
"""



[<AutoOpen>]
module ModelComponentAnswers =
    let answerStrongCausal = """
[sys] L = {
[flow] F = {
    Main = {
        Ap <|| Am;
        Ap ||> Am;
        Ap >> Am;
    }
}
[jobs] = {
    Ap = { A."+"(%Q1, %I1); }
    Am = { A."-"(%Q2, %I2); }
}
[device file="cylinder.ds"] A;
}
"""
    let answerButtons = """
[sys] My = {
[flow] F1 = {
    A > B;
}
[flow] F2 = {
    A > B;
}
[flow] F3 = {
    A > B;
}
[flow] F4 = {
    A > B;
}
[flow] F5 = {
    A > B;
}
[auto] = {
    AutoBTN = { F2; }
    AutoBTN2 = { F1;F3;F5; }
}
[emg] = {
    EmptyButton = { ; }
    EmptyButton2 = { ; }
    EMGBTN3 = { F3;F5; }
    EMGBTN = { F1;F2;F3;F5; }
}
[start] = {
    StartBTN_FF = { F2; }
    StartBTN1 = { F1; }
}
[reset] = {
    ResetBTN = { F1;F2;F3;F5; }
}
}
"""

    let answerT6Aliases = """
[sys ip = localhost] T6_Alias = {
[flow] Page1 = {
    C1 > C2;
    AndFlow.R2 > OrFlow.R1;
}
[flow] AndFlow = {
    R1 > R3;
    R2 > R3;
}
[flow] OrFlow = {
    R1 > R3;
    R2 > Copy1_R3;
    [aliases] = {
        R3 = { Copy1_R3; AliasToR3; }
        AndFlow.R3 = { AndFlowR3; OtherFlowR3; }
    }
}
[jobs] = {
    C1 = { B."+"(%Q1, %I1); A."+"(%Q1, %I1); }
    C2 = { A."-"(%Q3, _); B."-"(%Q3, _); }
}
[external file="cylinder.ds"] A;
[device file="cylinder.ds"] B;
}
"""
