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

        //Tester.DoSampleTestVps();
        //Tester.DoSampleTest();
        //Tester.DoSampleTestAdvanceReturn();
        //Tester.DoSampleTestHatOnHat();
        //Tester.DoSampleTestDiamond();
        //Tester.DoSampleTestTriangle();
        //Tester.DoSampleTestAddressesAndLayouts();

        ParserTest.Test(ParserTest.Safety, "Cpu");
        ParserTest.Test(ParserTest.StrongCausal, "Cpu");
        ParserTest.Test(ParserTest.Buttons, "Cpu");
        ParserTest.Test(ParserTest.Dup, "Cpu");
        ParserTest.Test(ParserTest.Ppt, "Cpu");
        ParserTest.Test(ParserTest.Serialize, "Cpu");
        ParserTest.Test(ParserTest.QualifiedName);
        ParserTest.Test(ParserTest.Aliases);
        ParserTest.Test(ParserTest.Serialize, "Cpu");

        ParserTest.Test(ParserTest.ExternalSegmentCall);
        ParserTest.Test(ParserTest.ExternalSegmentCallConfusing);
        ParserTest.Test(ParserTest.Error, "Cpu");

        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupSystemNameModel);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupFlowNameModel);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupParentingModel1);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupParentingModel2);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupParentingModel3);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupCallPrototypeModel);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupParentingWithCallPrototypeModel);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupCallTxModel);
    }
}
