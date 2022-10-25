namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser

[<AutoOpen>]
module private ModelComparisonHelper =
    let (=~=) (xs:string) (ys:string) =
        let toArray (xs:string) = xs.SplitByLine() |> Seq.where(fun x -> x.Trim().Any()) |> Array.ofSeq
        let xs = toArray xs
        let ys = toArray ys
        xs.Length === ys.Length
        for (x, y) in Seq.zip xs ys do
            if x <> y then
                failwithf "[%s] <> [%s]" x y

    [<AutoOpen>]           
    module ModelAnswers =
        let answerEveryScenarioText = """
    [sys ip = 192.168.0.1] My = {
        [flow] MyFlow = {
            Seg1 > Seg2;
            Seg1 = {
                A."+" > A."-";
            }
        }
        [flow] "Flow.Complex" = {
            "#Seg.Complex#" > Seg;
            "#Seg.Complex#" = {
                A."+" > A."-";
            }
        }
        [flow] F = {
            C1 > C3 > C5 > C6;
            C1 > C4 > C5;
            C2 > C3;
            C2 > C4;
            Main > R3;
            R1 > Main2 > Ap1;
            C3 |> C5;
            C4 |> C5;
            Main = {
                Ap1 > Am1 > Bm1 > Ap2 > Am2 > Bm2 > A."+";
                Ap2 > Bp2 > Bm2;
                Ap1 > Bp1 > Bm1;
            }
            R2; // island
            [aliases] = {
                A."+" = { Ap1; Ap2; }
                A."-" = { Am1; Am2; }
                B."+" = { Bp1; Bp2; }
                B."-" = { Bm1; Bm2; }
                Main = { Main2; }
            }
        }
        [emg] = {
            EMGBTN = { F; }
        }
    }
    [sys ip = 1.2.3.4] A = {
        [flow] F = {
            Vp > Pp > Sp;
            Vm > Pm > Sm;
            Vp |> Pm |> Sp;
            Vm |> Pp |> Sm;
            Vp <||> Vm;
        }
        [interfaces] = {
            "+" = { A.F.Vp ~ A.F.Sp }
            "-" = { A.F.Vm ~ A.F.Sm }
            "+" <||> "-";
        }
    }
    [sys ip = 1.2.3.4] B = {
        [flow] F = {
            Vp > Pp > Sp;
            Vm > Pm > Sm;
            Vp |> Pm |> Sp;
            Vm |> Pp |> Sm;
            Vp <||> Vm;
        }
        [interfaces] = {
            "+" = { B.F.Vp ~ B.F.Sp }
            "-" = { B.F.Vm ~ B.F.Sm }
            "+" <||> "-";
        }
    }
    [sys ip = 1.2.3.4] C = {
        [flow] F = {
            Vp > Pp > Sp;
            Vm > Pm > Sm;
            Vp |> Pm |> Sp;
            Vm |> Pp |> Sm;
            Vp <||> Vm;
        }
        [interfaces] = {
            "+" = { C.F.Vp ~ C.F.Sp }
            "-" = { C.F.Vm ~ C.F.Sm }
            "+" <||> "-";
        }
    }
    [prop] = {
        [safety] = {
            My.F.Main = { A.F.Sp; A.F.Sm; B.F.Sp; B.F.Sm; C.F.Sp; }
        }
        [addresses] = {
            A."+" = ( %Q1234.2343, %I1234.2343)
            A."-" = ( START, END)
        }
        [layouts] = {
            A."+" = (1309, 405, 205, 83)
        }
    }
    """
        let answerCodeElementsText = """
    [sys] My = {
        [flow] F = {
            Seg1; // island
        }
    }
    [variables] = {
        R100 = @(word, 0)
        R101 = @(word, 0)
        R102 = @(word, 5)
        R103 = @(dword, 0)
        PI = @(float, 3.1415)
    }
    [commands] = {
        CMD1 = @(Delay = 0)
        CMD2 = @(Delay = 30)
        CMD3 = @(add = 30, 50 ~ R103)
    }
    [observes] = {
        CON1 = @(GT = R102, 5)
        CON2 = @(Delay = 30)
        CON3 = @(Not = Tag1)
    }
    """

        let answerAdoptoedValidText = """
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
    }
    [sys] A = {
        [flow] F = {
            Vp > Pp > Sp;
            Vm > Pm > Sm;
            Vp |> Pm |> Sp;
            Vm |> Pp |> Sm;
            Vp <||> Vm;
        }
        [interfaces] = {
            "+" = { A.F.Vp ~ A.F.Sp }
            "-" = { A.F.Vm ~ A.F.Sm }
            "+" <||> "-";
        }
    }
    """
        let answerSplittedMRIEdgesText = """
    [sys] A = {
        [flow] F = {
            a1 > a2 > a3 > a4;
            a1 <||> a2;
            a2 <||> a3;
            a3 <||> a4;
            //a3 <||> a1;
            //a4 <||> a1;
            //a4 <||> a2;
        }
        [interfaces] = {
            I1 = { A.F.a1 ~ A.F.a2 }
            I2 = { A.F.a2 ~ A.F.a3 }
            I3 = { A.F.a3 ~ A.F.a1 }
            I1 <||> I2;
            I2 <||> I3;
            I3 ||> I4;
        }
    }
    """
        let answerDuplicatedEdgesText = """
    [sys] B = {
        [flow] F = {
            Vp > Pp;
            Vp |> Pp;
        }
    }
    """

    [<AutoOpen>]           
    module ModelComponentAnswers =
        let answerSafetyValid = """
[sys] L = {
    [flow] F = {
        Main = {
            Cp > Cm;
        }
        Main; // island
        [aliases] = {
            C.P = { Cp; Cp1; Ap2; }
            C.M = { Cm; Cm1; Cm2; }
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
        P = { C.F.Vp ~ C.F.Sp }
        M = { C.F.Vm ~ C.F.Sm }
        P <||> M;
    }
}
[prop] = {
    [safety] = {
        L.F.Main = { C.F.Sp; C.F.Sm; C.F.Vp; C.F.Vm; }
    }
}
"""
        let answerStrongCausal = """
[sys] L = {
    [flow] F = {
        Main = {
            Cp >> Cm;
            Cp <||> Cm;
        }
        Main; // island
        [aliases] = {
            A.P = { Cp; Cp1; Ap2; }
            A.M = { Cm; Cm1; Cm2; }
        }
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
        P = { A.F.Vp ~ A.F.Sp }
        M = { A.F.Vm ~ A.F.Sm }
        P <||> M;
    }
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
        let answerDup = """
[sys] L = {
    [flow] FF = {
        A > C;
        "F2.R2" > C;
        C |> "F2.R2";
    }
}
"""
        let answerQualifiedName = """
[sys] my.favorite.system!! = {
    [flow] " my flow. " = {
        R1 > R2;
        C1 = {
            EX."이상한. Api" > EX."Dummy. Api";
        }
        C1; // island
    }
}
[sys] EX = {
    [flow] F = {
        TX; // island
        "R.X"; // island
        "NameWith\"Quote"; // island
    }
    [interfaces] = {
        "이상한. Api" = { EX.F.TX ~ EX.F."R.X" }
        "Dummy. Api" = { _ ~ _ }
    }
}
"""
        let answerAliases = """
[sys] my = {
    [flow] F = {
        Main = {
            Ap1 > Am1 > Am2;
            Ap1 > Ap2 > Am2;
            Ap1 <||> Am1;
        }
        Main; // island
        [aliases] = {
            A."+" = { Ap1; Ap2; Ap3; }
            A."-" = { Am1; Am2; Am3; }
        }
    }
}
[sys] A = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;
        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <||> Vm;
    }
    [interfaces] = {
        "+" = { A.F.Vp ~ A.F.Sp }
        "-" = { A.F.Vm ~ A.F.Sm }
        "+" <||> "-";
    }
}
"""
        let answerQualifiedName2 = """
"""
        let answerQualifiedName3 = """
"""

    let compare originalText answer =
        let helper = ModelParser.ParseFromString2(originalText, ParserOptions.Create4Simulation())
        let model = helper.Model

        let generated = model.ToDsText();
        generated =~= answer

[<AutoOpen>]
module ModelTests1 =
    type DemoTests1() = 
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``Model parser test`` () =
            logInfo "============== Model parser test"

            logInfo "=== EveryScenarioText"
            compare Program.EveryScenarioText answerEveryScenarioText

            logInfo "=== CodeElementsText"
            compare Program.CodeElementsText answerCodeElementsText

            logInfo "=== AdoptoedValidText"
            compare Program.AdoptoedValidText answerAdoptoedValidText

            logInfo "=== DuplicatedEdgesText"
            compare Program.DuplicatedEdgesText answerDuplicatedEdgesText

            logInfo "=== SplittedMRIEdgesText"
            compare Program.SplittedMRIEdgesText answerSplittedMRIEdgesText

            logInfo "=== AdoptoedAmbiguousText"
            (fun () -> compare Program.AdoptoedAmbiguousText "")
                |> ShouldFailWithSubstringT "Ambiguous entry [F.Seg1] and [My.F.Seg1]"


        [<Test>]
        member __.``Model component test done`` () =
            compare ParserTest.SafetyValid answerSafetyValid
            compare ParserTest.StrongCausal answerStrongCausal
            compare ParserTest.Buttons answerButtons
            compare ParserTest.Dup answerDup
                
        [<Test>]
        member __.``Model component test`` () =

            //compare ParserTest.Ppt);
            compare ParserTest.Aliases answerAliases
            //compare ParserTest.ExternalSegmentCall ""
            //compare ParserTest.ExternalSegmentCallConfusing ""
            //compare ParserTest.MyFlowReference ""
            //compare ParserTest.Error ""
            compare ParserTest.QualifiedName answerQualifiedName        // todo : C1 island 없어야 ...
            
            
            