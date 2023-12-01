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
 
    let answerEveryScenarioText = """
[sys] My = {
    [flow] MyFlow = {
        Seg1 > Seg2; 		// Seg1(Real)> Seg2(Real)
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
        R1 > Main2;		// R1(Real)> Main2(Alias) > Ap1(Alias);
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
        [layouts] = {
            A = (1309, 405, 205, 83);
            B."+" = (1400, 300, 300, 300);
            B."-" = (1600, 500, 300, 300);
        }
        [finish] = {
            F.R2;
            F.R1;
        }
        [disable] = {
            F.Main.Ap;
        }
    }
    [device file="cylinder.ds"] A; // D:/Git/ds-Master/DsDotNet/src/UnitTest/UnitTest.Model/cylinder.ds
    [device file="cylinder.ds"] B; // D:/Git/ds-Master/DsDotNet/src/UnitTest/UnitTest.Model/cylinder.ds
    [external file="station.ds"] C; // D:/Git/ds-Master/DsDotNet/src/UnitTest/UnitTest.Model/station.ds
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
        I1 <||> I3;
        I1 <||> I4;
        I2 <||> I3;
        I2 <||> I4;
        I3 <||> I4;
    }
}
"""

    let answerSafetyValid = """
[sys] L = {
    [flow] F = {
        Ap > Main;		
        Am > Main2;	     
	                    
        Main = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
        Main2 = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
    }
    [jobs] = {
        Am = { A."-"(%I2, %Q2); }
        Ap = { A."+"(%I1, %Q1); }
    }
    [prop] = {
        [safety] = {
        F.Main = { F.Ap; F.Am; }
        F.Ap = { F.Main; }
        }
    }
    [device file="cylinder.ds"] A; // D:\ds\dsA\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
}
"""
    let answerLayoutValid = """
[sys] L = {
    [flow] F = {
        Ap > Main;		// Ap(Call)> Main(Real);
        Am > Main2;		// Am(Call)> Main2(Real);
        Main = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
        Main2 = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
    }
    [jobs] = {
        Am = { A."-"(%I2, %Q2); }
        Ap = { A."+"(%I1, %Q1); }
        Bm = { B."-"(%I4, %Q4); }
        Bp = { B."+"(%I3, %Q3); }
    }
    [prop] = {
        [layouts] = {
            A = (945, 123, 45, 67);
            B."+" = (1244, 254, 56, 54);
        }
    }
    [device file="cylinder.ds"] A; // E:\projects\dualsoft\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
    [device file="cylinder.ds"] B; // E:\projects\dualsoft\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
}
"""
    let answerFinishValid = """
[sys] L = {
    [flow] F = {
        Ap > Main;		// Ap(Call)> Main(Real);
        Am > Main2;		// Am(Call)> Main2(Real);
        Main = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
        Main2 = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
    }
    [jobs] = {
        Am = { A."-"(%I2, %Q2); }
        Ap = { A."+"(%I1, %Q1); }
    }
    [prop] = {
        [finish] = {
            F.Main;
            F.Main2;
        }
    }
    [device file="cylinder.ds"] A; // E:\projects\dualsoft\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
}
"""
    let answerDisableValid = """
[sys] L = {
    [flow] F = {
        Ap > Main;		
        Am > Main2;	     
	                    
        Main = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
        Main2 = {
            Ap > Am;		// Ap(Call)> Am(Call);
        }
    }
    [jobs] = {
        Am = { A."-"(%I2, %Q2); }
        Ap = { A."+"(%I1, %Q1); }
    }
    [prop] = {
        [disable] = {
            F.Main.Am;
            F.Main2.Am;
        }
    }
    [device file="cylinder.ds"] A; // D:\ds\dsA\DsDotNet\src\UnitTest\UnitTest.Engine\Model/../../UnitTest.Model/cylinder.ds
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
            AirOn1(%I1, _) = { F1;F2; }
            AirOn2(%I2, _) = { F1; }
        }
        [r] = {
            LeakErr(%I3, _) = { F2; }
            LeakErr.func = {
                $t 2000;
                $c 5;
            }
        }
    }
}
//DS Language Version = [0.9.2.12]
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
            AutoMode(_, %Q1) = { F1; }
        }
        [m] = {
            ManualMode(_, %Q1) = { F2; }
        }
        [d] = {
            RunMode(_, %Q1) = { F3; }
        }
        [s] = {
            StopMode(_, %Q1) = { F3; }
        }
        [e] = {
            EmgMode(_, %Q1) = { F3; }
        }
        [t] = {
            TestMode(_, %Q1) = { F5; }
        }
        [r] = {
            ReadyMode(_, %Q1) = { F4; }
            ReadyMode.func = {
                $t 2000;
                $c 5;
            }
        }
        [i] = {
            IdleLamp(_, %Q1) = { F5; }
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
            EMGBTN3(_, _) = { F3; }
            EMGBTN(_, _) = { F1;F2;F4;F5; }
        }
        [t] = {
            StartTestBTN(_, _) = { F5; }
        }
        [h] = {
            HomeBTN(_, _) = { F1;F2;F3;F5; }
            HomeBTN.func = {
                $t 2000;
                $c 5;
            }
        }
    }
}
"""

    let answerTaskLinkorDevice = """
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
        mv1up = { A."+"(%I300, %Q300); }
        mv1dn = { A."-"(%I301, %Q301); }
        FWD = sysR.RUN;
        BWD = sysR.RUN;
    }
    [interfaces] = {
        G = { F.Main ~ F.Main }
        R = { F.Reset ~ F.Reset }
        G <||> R;
    }
    [device file="cylinder.ds"] A;
    [external file="systemRH.ds"] sysR;
    [external file="systemLH.ds"] sysL;
}
"""

    let answerT6Alias = """
[sys] T6_Alias = {
    [flow] Page1 = {
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
            R3 = { Copy1_R3; }
        }
    }
    [jobs] = {
        C1 = { B."+"(%I1, %Q1); A."+"(_, %Q999.2343); }
        C1.func = {
            $t 2000;
            $c 5;
        }
        C2 = { A."-"(_, %Q3); B."-"(%I1, _); }
    }
    [device file="cylinder.ds"] B;
    [external file="cylinder.ds"] A;
    // [device file=c:/my.a.b.c.d.e.ds] C;      //<-- illegal: file path without quote!!
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
    [external file="systemRH.ds"] sysR;
    [external file="systemLH.ds"] sysL;
}
"""

    let linkAndLinkAliases = """
[sys] Control = {
    [flow] F = {
		Main = { mv1up, mv2dn > mv1dn, mv2up; }
		FWD > BWD > Main;
		Main <||> Reset;
		
		[aliases] = {
			FWD = { FWD2; }
			BWD = { BWD2; }
		}
    }
    [interfaces] = {
        G = { F.Main ~ F.Main }
        R = { F.Reset ~ F.Reset }
        G <||> R;
    }
	[jobs] = {
		mv1up = { M1.Up(%I300, %Q300); }
		mv1dn = { M1.Dn(%I301, %Q301); }
		mv2up = { M2.Up(%I302, %Q302); }
		mv2dn = { M2.Dn(%I303, %Q303); }
		FWD = Mt.fwd;
		BWD = Mt.bwd;
	}
    [external file=""HmiCodeGenExample/test_sample/device/MovingLifter1.ds""] M1;
    [external file=""HmiCodeGenExample/test_sample/device/MovingLifter2.ds""] M2;
	[external file=""HmiCodeGenExample/test_sample/device/motor.ds""] Mt;
}
"""
