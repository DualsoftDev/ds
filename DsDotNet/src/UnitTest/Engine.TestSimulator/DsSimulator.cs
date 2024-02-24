using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Cpu.RunTime;


namespace Engine.TestSimulator
{
    public static class DsSimulator
    {
        public static bool Do(DsCPU dsCpu, int checkTime = 2000)
        {
            RuntimeDS.Package = RuntimePackage.Simulation;
            bool resultMoving = false;    
            Task.Run(async () => {

                dsCpu.MySystem.Flows.Iter(f =>
                {
                    var reals = f.Graph.Vertices.OfType<Real>();
                    if (reals.Any())
                        ((VertexManager)reals.First().TagManager).SF.Value = true;
                });
                dsCpu.RunInBackground();

                resultMoving = await CheckEventCountAsync(dsCpu, checkTime);
            }).Wait();

            return resultMoving;
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


