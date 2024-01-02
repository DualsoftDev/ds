using Diagram.View.MSAGL;
using Dual.Common.Core;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;
using static Engine.Import.Office.ImportViewModule;
using static Engine.Import.Office.ViewModule;

namespace DsWebApp.Simulatior
{
    public static class DsSimulator
    {
        public static bool Do(DsSystem dsSys, DsCPU dsCpu)
        {
            List<ViewNode> nodeFlows = ImportViewUtil.GetViewNodesLoadingsNThis(dsSys).ToList();
            FormDocViewSim simView = new(dsSys, dsCpu);
            ViewUtil.ViewChangeSubject();
            ViewUtil.ViewInit(dsSys, nodeFlows);

            Dictionary<ViewNode, UcView> dicView = new();
            nodeFlows.Iter(f =>
            {
                dicView[f] = new UcView();
                dicView[f].SetGraph(f, f.Flow.Value, false);
            });

            ViewUtil.UcViews = dicView.Values.ToList();
            simView.ShowGraph(dsSys, dicView, dsSys.Flows.First().Name);
            _ = simView.ShowDialog();

            return true;
        }
    }
}


