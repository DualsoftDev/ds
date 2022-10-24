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
        for (x, y) in Seq.zip xs ys do
            if x <> y then
                failwithf "[%s] <> [%s]" x y
           
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


    let compare originalText generated =
        let helper = ModelParser.ParseFromString2(originalText, ParserOptions.Create4Simulation())
        let model = helper.Model

        let reWritten = model.ToDsText();
        reWritten =~= generated

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
            
            
            