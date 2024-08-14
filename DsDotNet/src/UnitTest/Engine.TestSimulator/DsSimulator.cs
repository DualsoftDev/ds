using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Dual.Common.Core;
using Engine.Core;
using Engine.Cpu;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.SystemManagerModule;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.Interface;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Cpu.RunTime;


namespace Engine.TestSimulator
{
    public static class DsSimulator
    {
        public static bool Do(DsCPU dsCpu, int checkTime = 2000)
        {
            RuntimeDS.Package = RuntimePackage.PCSIM;
 

            bool resultMoving = false;    
            Task.Run(async () => {
                CpuExtensionsModule.preManualAction(dsCpu.MySystem);
                var homeBtn = (dsCpu.MySystem.TagManager as SystemManager).GetSystemTag(TagKindList.SystemTag.home_btn);
                homeBtn.BoxedValue = true;
                dsCpu.RunInBackground();
                var org = (dsCpu.MySystem.TagManager as SystemManager).GetSystemTag(Core.TagKindList.SystemTag.originMonitor);
                if (!await WaitForOriginMonitorAsync(org, checkTime))
                {
                    throw new TimeoutException($"OriginMonitor was not set within the timeout period of {checkTime} milliseconds.");
                }
                homeBtn.BoxedValue = false;

                CpuExtensionsModule.preAutoDriveAction(dsCpu.MySystem);
                dsCpu.MySystem.Flows.Iter(f =>
                {
                    var reals = f.Graph.Vertices.OfType<Real>();
                    if (reals.Any())
                        ((VertexTagManager)reals.First().TagManager).SF.Value = true;
                });

                resultMoving = await CheckEventCountAsync(dsCpu, checkTime);
            }).Wait();

            return resultMoving;
        }
        static async Task<bool> WaitForOriginMonitorAsync(IStorage org, int timeout)
        {
            int elapsedTime = 0;
            int delayInterval = 100;

            while (!(bool)org.BoxedValue)
            {
                if (elapsedTime >= timeout)
                {
                    return false;  // Timeout reached
                }

                await Task.Delay(delayInterval);
                elapsedTime += delayInterval;
                Console.WriteLine("Waiting for originMonitor");
            }

            return true;
        }

        static async Task<bool> CheckEventCountAsync(DsCPU dsCpu, int checkTime)
        {
            List<string> changedNames = new();

            var subscription = dsCpu.TagWebChangedFromCpuSubject.Subscribe(s =>
            {
                Console.WriteLine($"Name:{s.Name}\t Value:{s.Value}");
                changedNames.Add(s.Name); 
            });
            await Task.Delay(checkTime);
            subscription.Dispose();

            var groupedNames = changedNames.GroupBy(name => name);
            return groupedNames.Any(group => group.Count() > 2);
        }
    }
}


