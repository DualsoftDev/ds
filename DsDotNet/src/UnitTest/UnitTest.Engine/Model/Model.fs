namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS

[<AutoOpen>]
module private ModelComparisonHelper =
    let (=~=) (xs:string) (ys:string) =
        let toArray (xs:string) =
            xs.SplitByLine()
                .Select(fun x -> x.Trim())
                |> Seq.where(fun x -> x.Any() && not <| x.StartsWith("//"))
                |> Array.ofSeq
        let xs = toArray xs
        let ys = toArray ys
        for (x, y) in Seq.zip xs ys do
            if x.Trim() <> y.Trim() then
                failwithf "[%s] <> [%s]" x y
        xs.Length === ys.Length

    [<AutoOpen>]
    module ModelAnswers =
        let answerEveryScenarioText = """
    [sys ip = 192.168.0.1] My = {
        [flow] MyFlow = {
            Seg1 > Seg2 > A."+";
            Seg1 = {
                A."+" > A."-";
            }
        }
        [flow] "Flow.Complex" = {
            "#Seg.Complex#" => Seg;
            "#Seg.Complex#" = {
                A."+" > A."-";
            }
        }
        [flow] F = {
            R1 > Main2 > Ap1;
            Main > R3;
            C2 > C4 > C5;
            C2 > C3 > C5 > C6;
            C1 > C4 |> C5;
            C1 > C3 |> C5;
            Main = {
                Ap2 > Bp2 > Bm2 > A."+";
                Ap1 > Bp1 > Bm1 > Ap2 > Am2 > Bm2;
                Ap1 > Am1 > Bm1;
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
        [sys ip = 1.2.3.4] A = {
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
        [sys ip = 1.2.3.4] B = {
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
        [sys ip = 1.2.3.4] C = {
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
        [emg] = {
            EMGBTN = { F; }
        }
        [prop] = {
            [addresses] = {
                A."+" = ( %Q1234.2343, %I1234.2343)
                A."-" = ( START, END)
                B."+" = ( %Q4321.2343, %I4321.2343)
                B."-" = ( BSTART, BEND)
            }
        }
        [prop] = {
            [safety] = {
                My.F.Main = { A.F.Sp; A.F.Sm; B.F.Sp; B.F.Sm; C.F.Sp; }
            }
            [layouts] = {
                A."+" = (1309, 405, 205, 83)
            }
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
        [sys] A = {
            [flow] F = {
                //Vp <||> Vm |> Pp |> Sm;
                //Vp |> Pm |> Sp;
                //Vm > Pm > Sm;
                //Vp > Pp > Sp;
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
        A."+" > A."-" > B."+";
    }
    [sys] A = {
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
    [sys] B = {
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
        [aliases] = {
            C.P = { Cp; Cp1; Ap2; }
            C.M = { Cm; Cm1; Cm2; }
        }
    }
    [sys] C = {
        [flow] F = {
            Pm |> Sp;
            Pp |> Sm;
            Vp <||> Vm > Pm > Sm;
            Vp > Pp > Sp;
        }
        [interfaces] = {
            P = { F.Vp ~ F.Sp }
            M = { F.Vm ~ F.Sm }
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
        [safety] = {
            L.F.Main = { C.F.Vp; C.F.Vm; }
        }
    }
}
"""
        let answerStrongCausal = """
[sys] L = {
    [flow] F = {
        Main = {
            Cp <|| Cm;
            Cp ||> Cm;
            Cp >> Cm;
        }
        [aliases] = {
            A.P = { Cp; Cp1; Ap2; }
            A.M = { Cm; Cm1; Cm2; }
        }
    }
    [sys] A = {
        [flow] F = {
            Pm |> Sp;
            Pp |> Sm;
            Vp <||> Vm > Pm > Sm;
            Vp > Pp > Sp;
        }
        [interfaces] = {
            P = { F.Vp ~ F.Sp }
            M = { F.Vm ~ F.Sm }
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
        C |> "F2.R2" > C;
        A > C;
    }
}"""
        let answerQualifiedName = """
[sys] my.favorite.system!! = {
    [flow] " my flow. " = {
        R1 > R2;
        C1 = {
            EX."이상한. Api" > EX."Dummy. Api";
        }
    }
    [sys] EX = {
        [flow] F = {
            TX; // island
            "R.X"; // island
            "NameWith\"Quote"; // island
        }
        [interfaces] = {
            "이상한. Api" = { F.TX ~ F."R.X" }
            "Dummy. Api" = { _ ~ _ }
        }
    }
}
"""

        let answerT6Aliases = """
[sys ip = localhost] T6_Alias = {
    [flow] Page1 = {
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
}
"""
        let answerAliases = """
[sys] my = {
    [flow] F = {
        Main = {
            Ap1 <||> Am1;
            Ap1 > Ap2 > Am2;
            Ap1 > Am1 > Am2;
        }

        [aliases] = {
            A."+" = { Ap1; Ap2; Ap3; }
            A."-" = { Am1; Am2; Am3; }
        }
    }
    [sys] A = {
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
    [prop] = {
        [addresses] = {
            A."+" = ( %Q1234.2343, %I1234.2343)
            A."-" = ( START, END)
        }
    }
}
"""

    let compare referenceDir originalText answer =
        let helper = ModelParser.ParseFromString2(originalText, ParserOptions.Create4Simulation(referenceDir, "ActiveCpuName"))
        let system = helper.TheSystem.Value

        let generated = system.ToDsText();
        generated =~= answer


[<AutoOpen>]
module ModelTests1 =
    type DemoTests1() =
        do
            Fixtures.SetUpTest()
            ModelParser.Initialize()

        let compare = compare @$"{__SOURCE_DIRECTORY__}\..\Libraries"

        [<Test>]
        member __.``EveryScenarioText test`` () =
            logInfo "=== EveryScenarioText"
            compare Program.EveryScenarioText answerEveryScenarioText

        [<Test>]
        member __.``CodeElementsText test`` () =
            logInfo "=== CodeElementsText"
            compare Program.CodeElementsText answerCodeElementsText

        [<Test>]
        member __.``XAdoptoedValidText test`` () =
            logInfo "=== AdoptoedValidText"
            compare Program.AdoptoedValidText answerAdoptoedValidText

        [<Test>]
        member __.``DuplicatedEdgesText test`` () =
            logInfo "=== DuplicatedEdgesText"
            compare Program.DuplicatedEdgesText answerDuplicatedEdgesText

        [<Test>]
        member __.``DuplicatedCallsText test`` () =
            logInfo "=== DuplicatedCallsText"
            compare Program.DuplicatedCallsText answerDuplicatedCallsText

        [<Test>]
        member __.``SplittedMRIEdgesText test`` () =
            logInfo "=== SplittedMRIEdgesText"
            compare Program.SplittedMRIEdgesText answerSplittedMRIEdgesText

        [<Test>]
        member __.``AdoptoedAmbiguousText test`` () =
            logInfo "=== AdoptoedAmbiguousText"
            try
                compare Program.AdoptoedAmbiguousText ""
            with exn ->
                ["duplicated"; "Duplicated"; "Ambiguous entry"].Any(fun msg -> exn.Message.Contains msg) === true

        [<Test>]
        member __.``Model component [SafetyValid] test`` () =
            compare ParserTest.SafetyValid answerSafetyValid

        [<Test>]
        member __.``Model component [StrongCausal] test`` () =
            compare ParserTest.StrongCausal answerStrongCausal

        [<Test>]
        member __.``Model component [Buttons] test`` () =
            compare ParserTest.Buttons answerButtons

        [<Test>]
        member __.``Model component [Dup] test`` () =
            compare ParserTest.Dup answerDup

        [<Test>]
        member __.``Model component [Aliases] test`` () =
            compare ParserTest.Aliases answerAliases

        [<Test>]
        member __.``Model component [QualifiedName] test`` () =
            compare ParserTest.QualifiedName answerQualifiedName

        [<Test>]
        member __.``Model component [T6 alias] test`` () =
            compare ParserTest.T6Alias answerT6Aliases

        //[<Test>]
        //member __.``Model component test`` () =
        //    compare ParserTest.Ppt);
        //    compare ParserTest.ExternalSegmentCall ""
        //    compare ParserTest.ExternalSegmentCallConfusing ""
        //    compare ParserTest.MyFlowReference ""
        //    compare ParserTest.Error ""
        //    ()

        [<Test>]
        member __.``Model ERROR duplication test`` () =
            //(fun () -> compare InvalidDuplicationTest.DupSystemNameModel "") |> ShouldFailWithSubstringT "An item with the same key has already been added"
            //(fun () -> compare InvalidDuplicationTest.DupFlowNameModel "")   |> ShouldFailWithSubstringT "Duplicated"
            //(fun () -> compare InvalidDuplicationTest.DupParentingModel1 "") |> ShouldFailWithSubstringT "Duplicated"
            //(fun () -> compare InvalidDuplicationTest.DupParentingModel2 "") |> ShouldFailWithSubstringT "Duplicated"
            (fun () -> compare InvalidDuplicationTest.CyclicEdgeModel ""  )  |> ShouldFailWithSubstringT "Cyclic"

            // todo : Loop detection


    type RecursiveSystemTests1() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``RecursiveSystem test`` () =
            logInfo "=== RecursiveSystem"
            compare Program.RecursiveSystemText answerEveryScenarioText
