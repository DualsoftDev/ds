using Engine.Core;
using Engine.Info;
using Engine.Runtime;
using System;
using System.IO;
using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;

namespace DsWebApp.Simulator
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            string testFile = Path.Combine(AppContext.BaseDirectory
                , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip");


            //Web test 시에 RuntimePackage.StandardPC 설정 (flow auto drive ready web에서 시작조건 켜야함)
            RuntimeDS.Package = RuntimePackage.Simulation;
            //RuntimeDS.Package = RuntimePackage.StandardPC; 
            RuntimeModel runModel = new(testFile);

            DsSystem[] systems = new DsSystem[] { runModel.System };
            DSCommonAppSettings commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"));

            ModelCompileInfo mci = new(runModel.JsonPath, runModel.JsonPath);
            _ = DBLogger.InitializeLogWriterOnDemandAsync(commonAppSettings, systems, mci);
            _ = DsSimulator.Do(runModel.System, runModel.Cpu);
        }
    }
}
