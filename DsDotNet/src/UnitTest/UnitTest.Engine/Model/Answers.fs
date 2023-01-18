namespace T

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

         //   Ap1 > Bp1; // to text DS 시에 순서 아래줄이랑  바뀜
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
        Ap = { A."+"(%I1, %Q1); }
        Am = { A."-"(%I2, %Q2); }
        Bp = { B."+"(%I3, %Q3); }
        Bm = { B."-"(%I4, %Q4); }
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
        Seg1 > Seg2 > Ap;		// Seg1(Real)> Seg2(Real) > Ap(Call);
        Seg1 = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
    }
    [flow] "Flow.Complex" = {
        "#Seg.Complex#" => Seg;		// "#Seg.Complex#"(Real)=> Seg(Real);
        "#Seg.Complex#" = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
    }
    [flow] F = {
        R1 > Main2 > Ap1;		// R1(Real)> Main2(Alias) > Ap1(Alias);
        Main > R3;		// Main(Real)> R3(Real);
        C4 > C5;		// C4(Real)> C5(Real);
        C3 > C5 > C6;		// C3(Real)> C5(Real) > C6(Real);
        C1, C2 > C3, C4 |> C5;		// C1(Real), C2(Real)> C3(Real), C4(Real) |> C5(Real);
        Main = {
            Bm2 > Ap > Am > Bp > Bm;		// Bm2(Alias)> Ap(Call) > Am(Call) > Bp(Call) > Bm(Call);
            Ap2 > Bp2 > Bm2;		// Ap2(Alias)> Bp2(Alias) > Bm2(Alias);
            Ap1 > Bp1 > Bm1 > Ap2 > Am2 > Bm2;		// Ap1(Alias)> Bp1(Alias) > Bm1(Alias) > Ap2(Alias) > Am2(Alias) > Bm2(Alias);
            Ap1 > Am1 > Bm1;		// Ap1(Alias)> Am1(Alias) > Bm1(Alias);
        }
            R2; // island
        [aliases] = {
            Main.Ap = { Ap1; Ap2; }
            Main.Am = { Am1; Am2; }
            Main.Bp = { Bp1; Bp2; }
            Main.Bm = { Bm1; Bm2; }
            Main = { Main2; }
        }
    }
    [jobs] = {
        Ap = { A."+"(%I1, %Q1); }
        Am = { A."-"(%I2, %Q2); }
        Bp = { B."+"(%I3, %Q3); }
        Bm = { B."-"(%I4, %Q4); }
    }
    [buttons] = {
        [e] = {
            EMGBTN(_, _) = { F; }
        }
    }
    [prop] = {
        [safety] = {
            F.Main = { F.Main.Ap; }
            F.Main.Am = { F.Main; }
        }
    }
    [device file="cylinder.ds"] A; // D:\Git\ds-Master\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds
    [device file="cylinder.ds"] B; // D:\Git\ds-Master\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\cylinder.ds
    [external file="station.ds" ip="192.168.0.2"] C; // D:\Git\ds-Master\DsDotNet\src\UnitTest\UnitTest.Engine\Model\..\Libraries\station.ds
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
    Fp = { F."+"(%I1, %Q1); }
    Fm = { F."-"(%I2, %Q2); }
    Gm = { G."-"(%I3, %Q3); }
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
    Ap = { A."+"(%I1, %Q1); }
    Am = { A."-"(%I2, %Q2); }
}
[device file="cylinder.ds"] A;
}
"""
    let answerConditions = """
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
    [conditions] = {
        [d] = {
            AirOn1(%I1) = { F1;F2; }
            AirOn2(%I2) = { F1; }
        }
        [r] = {
            LeakErr(%I3) = { F2; }
            LeakErr.func = {
                $ton 2000;
                $ctu 1 5;
            }
        }
    }
}
"""
    let answerLamps= """
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
    [lamps] = {
        [a] = {
            AutoMode(%Q1) = { F1 }
        }
        [m] = {
            ManualMode(%Q1) = { F2 }
        }
        [d] = {
            RunMode(%Q1) = { F3 }
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
                $ton 2000;
                $ctu 1 5;
            }
        }
    }
}
"""

    let answerButtons = """
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
        [d] = {
            StartBTN_FF(_, _) = { F2; }
            StartBTN1(_, _) = { F1; }
        }
        [s] = {
            StopBTN(_, _) = { F1;F2;F5; }
        }
        [c] = {
            ClearBTN(_, _) = { F1;F2;F3;F5; }
        }
        [e] = {
            EmptyButton(_, _) = {  }
            EmptyButton2(_, _) = {  }
            EMGBTN3(_, _) = { F3;F5; }
            EMGBTN(_, _) = { F1;F2;F3;F5; }
        }
        [t] = {
            StartTestBTN(_, _) = { F5; }
        }
        [h] = {
            HomeBTN(_, _) = { F1;F2;F3;F5; }
            HomeBTN.func = { 
                $ton 2000; 
                $ctu 1 5; 
            }
        }
    }
}
"""
    let answerCircularDependency = """
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
    [external file="systemRH.ds" ip="localhost"] sysR;
    [external file="systemLH.ds" ip="localhost"] sysL;
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
        C1 = { B."+"(%I1, %Q1); A."+"(%I1, %Q1); }
        C1.func = {
            $ton 2000; 
            $ctu 1 5; 
        }
        C2 = { A."-"(_, %Q3); B."-"(_, %Q3); }
    }
    [device file="cylinder.ds"] B;
    [external file="cylinder.ds" ip="192.168.0.1"] A;
}
"""