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
using static Engine.Core.ModelConfigModule;
using static Engine.Import.Office.ImportPptModule;
using static Engine.Info.DBLoggerORM;
using static Engine.Info.DBWriterModule;
using Engine.Import.Office;
using Engine.CodeGenCPU;

namespace Engine.TestSimulator
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            string testFile = Path.Combine(AppContext.BaseDirectory
                , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/SampleA/exportDS/testA/testMy/my.pptx");
            var modelConfig = ModelConfigPPT.getModelConfigFromPPTFile(testFile);
            PptParams pptParms = new PptParams(modelConfig.HwTarget, true, modelConfig.HwTarget.StartMemory, OpModeLampBtnMemorySize);
            (string dsz, DsSystem _system) = ImportPpt.GetRuntimeZipFromPpt(testFile, pptParms);
            RuntimeModel runModel = new(dsz, HwCPU.WINDOWS);

            _ = DsSimulator.Do(runModel.Cpu);
            Console.ReadKey();
        }
    }
}
