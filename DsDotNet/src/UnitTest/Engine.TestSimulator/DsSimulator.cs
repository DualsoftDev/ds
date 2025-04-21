using DocumentFormat.OpenXml.Office2010.ExcelAc;

using Dual.Common.Base.CS;
using Dual.Common.Core;
using Engine.Core;
using Engine.Cpu;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.SystemManagerModule;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.CoreModule.GraphItemsModule;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.Interface;
using static Engine.Core.ModelConfigModule;
using static Engine.Cpu.RunTimeModule;


namespace Engine.TestSimulator
{
    public static class DsSimulator
    {
        public static bool Do(DsCPU dsCpu)
        {
            bool resultMoving = false;
            Task.Run(async () => {
                dsCpu.Run();
                var sm = dsCpu.MySystem.TagManager as SystemManager;
                CpuExtensionsModule.preManualAction(dsCpu.MySystem);
                var homeBtn = sm.GetSystemTag(TagKindList.SystemTag.home_btn);
                homeBtn.BoxedValue = true;

                int waitForOriginMonitorLimit = 5000;
                if (!await WaitForOriginMonitorAsync(sm.GetSystemTag(TagKindList.SystemTag.originMonitor), waitForOriginMonitorLimit))
                {
                    throw new TimeoutException($"OriginMonitor was not set within the timeout period of {waitForOriginMonitorLimit} milliseconds.");
                }

                homeBtn.BoxedValue = false;

                CpuExtensionsModule.preAutoDriveAction(dsCpu.MySystem);
                dsCpu.MySystem.Flows.Iter(f =>
                {
                    var reals = f.Graph.Vertices.OfType<Real>();
                    if (reals.Any())
                        ((VertexTagManager)reals.First().TagManager).SF.Value = true;
                });

                resultMoving = await CheckEventCountAsync(dsCpu);
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

        static async Task<bool> CheckEventCountAsync(DsCPU dsCpu)
        {
            List<string> changedNames = new();
            CancellationTokenSource cts = new();
            var subscription = dsCpu.TagWebChangedFromCpuSubject.Subscribe(s =>
            {
                Console.WriteLine($"Name:{s.Name}\t Value:{s.Value}");
                changedNames.Add(s.Name);

                var groupedNames = changedNames.GroupBy(name => name);
                if (groupedNames.Any(group => group.Count() > 2))
                {
                    cts.Cancel();  // Cancel the delay task if the condition is met
                }
            });

            try
            {
                await Task.Delay(10000, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled because the condition was met
                return true;
            }
            finally
            {
                subscription.Dispose();
            }

            
            return false;
        }

    }
}


