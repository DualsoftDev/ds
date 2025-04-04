using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using Microsoft.FSharp.Collections;
using System;
using System.IO;
using System.Threading.Tasks;

using static Engine.Core.MapperDataModule;
using static Engine.Core.CoreModule;
using static Engine.Core.CoreModule.SystemModule;
using static Engine.Core.LoaderModule;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Import.Office.ImportPptModule;
using static Engine.Info.DBLoggerORM;
using static Engine.Info.DBWriterModule;

namespace Engine.TestSimulator
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            string testFile = Path.Combine(AppContext.BaseDirectory
                , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/SampleA/exportDS/testA/testMy/my.pptx");
            var userTagConfig = createDefaultUserTagConfig(); 
            PptParams pptParms = new PptParams(getDefaltHwTarget(), userTagConfig, true, false, true, 1000, 100);
            var modelConfig = createDefaultModelConfig();
            (string dsz, DsSystem _system) = ImportPpt.GetRuntimeZipFromPpt(testFile, pptParms, modelConfig);
            RuntimeModel runModel = new(dsz, PlatformTarget.WINDOWS);
            _ = DsSimulator.Do(runModel.Cpu);
            Console.ReadKey();
        }
    }
}
