using DevExpress.Utils.Extensions;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Model.Import.Office.ImportPPTModule;

namespace DSModeler
{
    public static class PPT
    {

        public static async Task<Dictionary<DsSystem, PouGen>> ImportPowerPoint(string[] files, FormMain formMain)
        {
            Dictionary<DsSystem, PouGen> dicCpu = new Dictionary<DsSystem, PouGen>();
            var ret = ImportPPT.GetLoadingAllSystem(files);
            formMain.Model = ret.Item1;
            var PPTResults = ret.Item2;
            var storages = new Storages();
            int cnt = 0;

            var recentDocs = RecentDocs.GetRegistryRecentDocs();

            await formMain.DoAsync(async tsc =>
            {
                foreach (var ppt in PPTResults.OrderByDescending(o=>o.IsActive))
                {
                    if (ppt.IsActive)
                    {
                        await Task.Run(async () =>
                        {
                            var pous = Cpu.LoadStatements(ppt.System, storages).ToList();
                            foreach (var pou in pous)
                            {
                                dicCpu.Add(pou.ToSystem(), pou);
                                DsProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / pous.Count() * 50));
                                await Task.Delay(1);
                            }
                            await HMITree.CreateHMIBtn(formMain, ppt);
                            Global.ActiveSys = ppt.System;
                        });
                    }

                    ModelTree.CreateModelBtn(formMain, ppt);
                    ViewDraw.DrawInitStatus(dicCpu);

                    var nodeFlows = ppt.Views.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                   .Where(w => w.UsedViewNodes.Any())
                                   .Where(w => recentDocs.Contains(w.Flow.Value.QualifiedName));
                    
                    nodeFlows.Iter(f=> DocControl.CreateDocOrSelect(formMain, f));
                }



                formMain.Ace_Model.Expanded = true;
                formMain.Ace_System.Expanded = true;
                formMain.Ace_Device.Expanded = false;
                tsc.SetResult(true);
            });
            return dicCpu;
        }
    }
}


