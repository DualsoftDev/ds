using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using System;
using System.IO;
using System.Threading.Tasks;

using static Engine.Core.CoreModule;
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
            PptParams pptParms = new PptParams(PlatformTarget.WINDOWS, HwDriveTarget.LS_XGK_IO, true, false, true);
            (string dsz, DsSystem _system) = ImportPpt.GetRuntimeZipFromPpt(testFile, pptParms);

            RuntimeModel runModel = new(dsz, PlatformTarget.WINDOWS);
            _ = DsSimulator.Do(runModel.Cpu, 1000000);
            Console.ReadKey();
        }
    }
}
