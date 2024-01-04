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
        public static bool Do(DsCPU dsCpu)
        {
            var a = RuntimeDS.Package;
            bool resultMoving = false;    
            Task.Run(async () => {

                dsCpu.QuickDriveReady();
                dsCpu.MySystem.Flows.Iter(f =>
                {
                    var reals = f.Graph.Vertices.OfType<Real>();
                    if (reals.Any())
                        ((VertexManager)reals.First().TagManager).SF.Value = true;
                });
                dsCpu.RunInBackground();

                resultMoving = await CheckEventCountAsync(dsCpu);
            }).Wait();

            return resultMoving;
        }

        static async Task<bool> CheckEventCountAsync(DsCPU dsCpu)
        {
            List<string> changedNames = new();

            var subscription = dsCpu.TagWebChangedFromCpuSubject.Subscribe(s => changedNames.Add(s.Name));
            await Task.Delay(2000);
            subscription.Dispose();

            var groupedNames = changedNames.GroupBy(name => name);
            return groupedNames.Any(group => group.Count() > 2);
        }
    }
}


