using System;
using System.Reflection;
using System.Threading;

using Dual.Common.Base.CS;
using Dual.Common.Core;
using Dual.Common.Core.FS;

namespace Engine.Sample;

class Program
{



    static void PrepareThreadPool()
    {
        //ThreadPool.GetMinThreads(out int minWorker, out int minIOC);
        ThreadPool.SetMinThreads(60, 60);
    }

    static void Main(string[] args)
    {

        //PrepareThreadPool();
        SimpleExceptionHandler.InstallExceptionHandler();
        //DllVersionChecker.IsValidExDLL(Assembly.GetExecutingAssembly());
        var logger = DcLogger.PrepareLog4Net("UnitTestLogger");
        Log4NetWrapper.SetLogger(logger);

        logger.Info("Sample Runner started.");

        //Tester.DoSampleTestVps();
        //Tester.DoSampleTest();
        //Tester.DoSampleTestAdvanceReturn();
        //Tester.DoSampleTestHatOnHat();
        //Tester.DoSampleTestDiamond();
        //Tester.DoSampleTestTriangle();
        //Tester.DoSampleTestAddressesAndLayouts();

        //Engine.Parser.FS.Program.Main(null);
        SampleRunner.Run(Tester.DoSampleTestAddressesAndLayouts());
        //SampleRunner.Run(ParserTest.SafetyValid);
        //SampleRunner.Run(ParserTest.StrongCausal);
        //SampleRunner.Run(ParserTest.Buttons);
        //SampleRunner.Run(ParserTest.Dup);
        //SampleRunner.Run(ParserTest.Ppt);
        //SampleRunner.Run(ParserTest.QualifiedName);
        //SampleRunner.Run(ParserTest.Aliases);
        //SampleRunner.Run(ParserTest.Serialize);
        //SampleRunner.Run(ParserTest.ExternalSegmentCall);
        //SampleRunner.Run(ParserTest.ExternalSegmentCallConfusing);
        //SampleRunner.Run(ParserTest.MyFlowReference);
        //SampleRunner.Run(ParserTest.Error);

        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupSystemNameModel);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupFlowNameModel);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupParentingModel1);
        //InvalidDuplicationTest.Test(InvalidDuplicationTest.DupParentingModel2);
        Console.ReadKey();
    }
}
