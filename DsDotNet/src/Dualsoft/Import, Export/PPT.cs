using DevExpress.Utils.Extensions;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Engine.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Import.Office.ImportPPTModule;

namespace DSModeler
{
    public static class PPT
    {

        public static async Task<Dictionary<DsSystem, PouGen>> ImportPowerPoint(string[] files, FormMain formMain)
        {
            Dictionary<DsSystem, PouGen> dicCpu = new Dictionary<DsSystem, PouGen>();
            var ret = ImportPPT.GetLoadingAllSystem(files);
            formMain.Model = ret.Item1;
            var pptResults = ret.Item2;
            var storages = new Storages();
            int cnt = 0;

            var recentDocs = RecentDocs.GetRegistryRecentDocs();
            var activeSys = pptResults.First(f => f.IsActive).System;
            Global.ActiveSys = activeSys;


            var pous = Cpu.LoadStatements(activeSys, storages).ToList();
            var viewAll = pptResults.SelectMany(f => f.Views)
                                    .Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                    //.Where(w => w.UsedViewNodes.Any())
                                    .ToDictionary(s => s.Flow.Value, ss => ss);
            ModelTree.CreateActiveSystemBtn(formMain, activeSys, viewAll);

            foreach (var pou in pous)
            {
                var sys = pou.ToSystem();
                var viewSet = pptResults.First(f => f.System == sys).Views;

                dicCpu.Add(sys, pou);

                if (activeSys == sys) await HMITree.CreateHMIBtn(formMain, sys, viewSet);


                ViewDraw.DrawInitStatus(formMain.TabbedView, dicCpu);
                ViewDraw.DrawInitActionTask(formMain, dicCpu);

                var nodeFlows = viewSet.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                               .Where(w => w.UsedViewNodes.Any())
                               .Where(w => recentDocs.Contains(w.Flow.Value.QualifiedName));

                nodeFlows.Iter(f => DocControl.CreateDocOrSelect(formMain, f));

                DsProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / pous.Count() * 50));
                await Task.Delay(1);
            }


            formMain.Do(() =>
            {
                //formMain.Ace_Model.Expanded = false;
                formMain.Ace_System.Expanded = false;
                formMain.Ace_Device.Expanded = false;
            });
            //tsc.SetResult(true);
            //});
            return dicCpu;
        }
    }
}


