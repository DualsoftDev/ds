using Engine.Common.FS;
using System.Reflection;
using System.Threading;

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
        ParserTest.TestError2();
    }
}
