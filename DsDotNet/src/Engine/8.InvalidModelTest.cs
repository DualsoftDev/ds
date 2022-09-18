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
[sys] A = {
    [flow] F = {
        Root = {X > Y;}
        Root;
    }
}
";
        public static string DupParentingModel3 = @"
[sys] A = {
    [flow] F = {
        Root = {X > Y;}
        Root = {Ex.F.Tx ~ Ex.F.Rx}
    }
}
[sys] Ex = {}
";
        public static string DupCallPrototypeModel = @"
[sys] A = {
    [flow] F = {
        Root = {X > Y;}
        X = {Ex.F.Tx ~ Ex.F.Rx}
        X = {Ex.F.Tx ~ Ex.F.Rx}
    }
}
";

        public static string DupParentingWithCallPrototypeModel = @"
[sys] A = {
    [flow] F = {
        Root = {X > Y;}
        Root = {Ex.F.Tx ~ Ex.F.Rx}
        X = {Ex.F.Tx ~ Ex.F.Rx}
        Y = {Ex.F.Tx ~ Ex.F.Rx}
    }
}
[sys] Ex = {}
";

        public static string DupCallTxModel = @"
[sys] A = {
    [flow] F = {
        Root = {X > Y;}
        X = {Ex.F.Tx, Ex.F.Tx ~ Ex.F.Rx}
        Y = {Ex.F.Tx ~ Ex.F.Rx}
    }
}
[sys] Ex = {
    [flow] F = {
        Tx;
    }
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
            var engine = new EngineBuilder(text, ParserOptions.Create4Simulation()).Engine;
            Program.Engine = engine;
            engine.Run();
        }


    }
}
