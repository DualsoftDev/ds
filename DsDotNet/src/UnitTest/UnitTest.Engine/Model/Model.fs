namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module private ModelComparisonHelper =
    let (=~=) (xs:string) (ys:string) =
        let removeComment input =
            let blockComments = @"/\*(.*?)\*/"
            let lineComments = @"//(.*?)$"
            Regex.Replace(input, $"{blockComments}|{lineComments}", "", RegexOptions.Singleline)

        let toArray (xs:string) =
            xs.SplitByLine()
                .Select(removeComment)
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
            C2 > C4 > C5;
            C2 > C3 > C5 > C6;
            C1 > C4 |> C5;
            C1 > C3 |> C5;
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
        [calls] = {
            Ap = { A."+"(%Q1, %I1); }
            Am = { A."-"(%Q2, %I2); }
            Bp = { B."+"(%Q3, %I3); }
            Bm = { B."-"(%Q4, %I4); }
        }
        [device file="cylinder.ds"] A;
        [device file="cylinder.ds"] B;
        [external file="station.ds"] C;
        [emg] = {
            EMGBTN = { F; }
        }
        [prop] = {
            [layouts] = {
                Ap = (1309, 405, 205, 83)
            }
            //[addresses] = {
            //    A."+" = ( %Q1234.2343, %I1234.2343)
            //    A."-" = ( START, END)
            //    B."+" = ( %Q4321.2343, %I4321.2343)
            //    B."-" = ( BSTART, BEND)
            //}
        }
        //[prop] = {
        //    [safety] = {
        //        My.F.Main = { A.F.Sp; A.F.Sm; B.F.Sp; B.F.Sm; C.F.Sp; }
        //    }
        //}
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
    [calls] = {
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
            Ap <|| Am;
            Ap ||> Am;
            Ap >> Am;
        }
    }
    [calls] = {
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
        let answerDup = """
[sys] L = {
    [flow] FF = {
        C |> Ap > C;
        A > C;
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [device file="cylinder.ds"] A;
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
    [calls] = {
        C1 = { B."+"(%Q1, %I1); A."+"(%Q1, %I1); }
        C2 = { A."-"(%Q3, _); B."-"(%Q3, _); }
    }
    [external file="cylinder.ds"] A;
    [device file="cylinder.ds"] B;
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
            Ap = { Ap1; Ap2; Ap3; }
            Am = { Am1; Am2; Am3; }
        }
    }
    [calls] = {
        Ap = { A."+"(%Q1, %I1); }
        Am = { A."-"(%Q2, %I2); }
    }
    [device file="cylinder.ds"] A;
}
"""

    let compare referenceDir originalText answer =
        let helper = ModelParser.ParseFromString2(originalText, ParserOptions.Create4Simulation(referenceDir, "ActiveCpuName"))
        let system = helper.TheSystem.Value

        validateSystem system

        let generated = system.ToDsText();
        generated =~= answer


[<AutoOpen>]
module ModelTests1 =
    [<AbstractClass>]
    type TestBase() =
        do
            Fixtures.SetUpTest()
            ModelParser.Initialize()

    type DemoTests1() =
        inherit TestBase()

        let compare = compare @$"{__SOURCE_DIRECTORY__}\..\Libraries"

        [<Test>]
        member __.``0 Any test`` () =
            logInfo "=== 0 Any test"
            let input = """
[sys] EX = {
    [flow] F = {
        TX;
        "R.X";
        "NameWith\"Quote";
    }
    //[interfaces] = {
    //    "이상한. Api" = { F.TX ~ F."R.X" }
    //    "Dummy. Api" = { _ ~ _ }
    //}
}
"""
            compare input input

        [<Test>]
        member __.``EveryScenarioText test`` () =
            logInfo "=== EveryScenarioText"
            compare Program.EveryScenarioText answerEveryScenarioText

        [<Test>]
        member __.``CodeElementsText test`` () =
            logInfo "=== CodeElementsText"
            compare Program.CodeElementsText Program.CodeElementsText

        [<Test>]
        member __.``AdoptoedValidText test`` () =
            logInfo "=== AdoptoedValidText"
            compare Program.AdoptoedValidText Program.AdoptoedValidText

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
        member __.``SimpleLoadedDeviceText test`` () =
            logInfo "=== SimpleLoadedDeviceText"
            compare Program.SimpleLoadedDeviceText Program.SimpleLoadedDeviceText

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
            compare ParserTest.QualifiedName ParserTest.QualifiedName

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


    type InvalidModelTests1() =
        inherit TestBase()

        let compare = compare @$"{__SOURCE_DIRECTORY__}\..\Libraries"

        [<Test>]
        member __.``RecursiveSystem test`` () =
            logInfo "=== RecursiveSystem"
            (fun () -> compare Program.RecursiveSystemText "" ) |> ShouldFail
