using Dual.Common.Core;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.FlowManagerModule;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;
using static Engine.Import.Office.ImportViewModule;
using static Engine.Import.Office.ViewModule;

namespace DsWebApp.Simulator
{
    public static class DsSimulator
    {
        public static bool Do(DsSystem dsSys, DsCPU dsCpu)
        {
            dsCpu.MySystem.Flows.Iter(f =>
            {
                var reals = f.Graph.Vertices.OfType<Real>();
                if (reals.Any())
                    ((VertexManager)reals.First().TagManager).SF.Value = true;
            });

            dsCpu.Run();
           

            return true;
        }
    }
}


