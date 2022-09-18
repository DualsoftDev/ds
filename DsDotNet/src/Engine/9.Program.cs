using Engine.Common.FS;
using System.Reflection;
using System.Threading;

using static System.Net.Mime.MediaTypeNames;

namespace Engine;

class Program
{
    public static ENGINE Engine { get; set; }



    static void PrepareThreadPool()
    {
        //ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
        ThreadPool.SetMinThreads(60, 60);
    }

    static void Main(string[] args)
    {

        //PrepareThreadPool();
        SimpleExceptionHandler.InstallExceptionHandler();
        DllVersionChecker.IsValidExDLL(Assembly.GetExecutingAssembly());
        var logger = Log4NetHelper.PrepareLog4Net("EngineLogger");
        Log4NetWrapper.SetLogger(logger);
        Global.Logger = logger;

        void Test(string text)
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

        //Tester.DoSampleTestVps();
        //Tester.DoSampleTest();
        //Tester.DoSampleTestAdvanceReturn();
        //Tester.DoSampleTestHatOnHat();
        //Tester.DoSampleTestDiamond();
        //Tester.DoSampleTestTriangle();
        //Tester.DoSampleTestAddressesAndLayouts();


        //ParserTest.TestParseSafety();
        //ParserTest.TestParseStrongCausal();
        //ParserTest.TestParseButtons();
        //ParserTest.TestParsePpt();
        //ParserTest.TestSerialize();
        //ParserTest.TestError();
        //ParserTest.TestParseQualifiedName();
        //ParserTest.TestParseExternalSegmentCall();
        //ParserTest.TestParseAliases();

        //Test(InvalidDuplicationTest.DupSystemNameModel);
        //Test(InvalidDuplicationTest.DupFlowNameModel);
        //Test(InvalidDuplicationTest.DupParentingModel1);
        //Test(InvalidDuplicationTest.DupParentingModel2);
        //Test(InvalidDuplicationTest.DupParentingModel3);
        //Test(InvalidDuplicationTest.DupCallPrototypeModel);
        //Test(InvalidDuplicationTest.DupParentingWithCallPrototypeModel);
        Test(InvalidDuplicationTest.DupCallTxModel);
    }
}
