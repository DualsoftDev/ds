using System.IO;
using System.Configuration;
using Engine.Common.FS;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;

namespace Engine;

class Program
{
    public static ILog Logger { get; private set; }
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
        Global.Logger = Log4NetHelper.PrepareLog4Net("EngineLogger");
        //Tester.DoSampleTestVps();
        //Tester.DoSampleTest();
        //Tester.DoSampleTestAdvanceReturn();
        //Tester.DoSampleTestHatOnHat();
        Tester.DoSampleTestDiamond();
        //Tester.DoSampleTestTriangle();
        //Tester.DoSampleTestAddressesAndLayouts();


        //ParserTest.TestParseSafety();
        //ParserTest.TestParseFlowTask();
    }
}
