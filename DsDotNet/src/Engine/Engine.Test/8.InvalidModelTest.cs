namespace Engine
{
    internal class InvalidDuplicationTest
    {
        public static string DupSystemNameModel = @"
[sys] A = {}
[sys] A = {}
";

        public static string DupFlowNameModel = @"
[sys] A = {
    [flow] F = {}
    [flow] F = {}
}
";
        public static string DupParentingModel1 = @"
[sys] A = {
    [flow] F = {
        Root = {X > Y;}
        Root = {X > Y;}
    }
}
";
        public static string DupParentingModel2 = @"
[sys] My = {
    [flow] F = {
        Root = {A.Plus > A.Minus;}
        Root;
    }
}
[sys] A = {
    [flow] F = {
        Ap > Am;
    }
    [interfaces] = {
        Plus = { F.Ap ~ F.Am }
        Minus = { F.Am ~ F.Ap }
    }
}
";

        public static string CyclicEdgeModel = @"
[sys] My = {
    [flow] F = {
        Main = {
            Ap > Am;
            Am > Ap;
        }
    }
    [jobs] = {
        Ap = { A.""+""(%I1, %Q1); }
        Am = { A.""-""(%I2, %Q2); }
    }
    [device file=""cylinder.ds""] A;
}
";
        public static string DupButtonCategory = @"
[sys] My = {
    [emg] = {
    }
    [emg] = {
    }
}
";

        public static string DupButtonName = @"
[sys] My = {
    [emg] = {
        EmptyButton = {};
        EmptyButton = {}
    }
}
";

        public static string DupButtonFlowName = @"
[sys] My = {
    [flow] F1 = { A > B; }
    [emg] = {
        EmptyButton = {F1; F1};
    }
}
";


        public static void Test(string text)
        {
            if (!text.Contains("[cpus]"))
                text += @"
[cpus] AllCpus = {
    [cpu] DummyCpu = {
        X.Y;
    }
}
";

        }


    }
}
