using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using System;
using System.IO;
using System.Threading.Tasks;

using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;

namespace Engine.TestSimulator
{
    internal class Program
    {
        [STAThread]
        private static async Task Main()
        {
            string testFile = Path.Combine(AppContext.BaseDirectory
                , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip");


            //Web test 시에 RuntimePackage.PC 설정 (flow auto drive ready web에서 시작조건 켜야함)
            RuntimeDS.Package = RuntimePackage.PCSIM;
           // RuntimeDS.Package = RuntimePackage.PC; 
            RuntimeModel runModel = new(testFile, PlatformTarget.WINDOWS);

            DsSystem[] systems = new DsSystem[] { runModel.System };

            ModelCompileInfo mci = new(runModel.JsonPath, runModel.JsonPath);
            DSCommonAppSettings commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));
            var queryCriteria = new QueryCriteria(commonAppSettings, -1, DateTime.Now.Date.AddDays(-1), null);
            _ = await DBLogger.InitializeLogWriterOnDemandAsync(queryCriteria, systems, mci, false);
            _ = DsSimulator.Do(runModel.Cpu, 10000);
            Console.ReadKey();  
        }
    }
}
