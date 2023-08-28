using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;

using Dual.Common.Winform;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Cpu.RunTime;
using static Engine.Cpu.RunTimeUtil;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class PPT
    {

        public static async Task<bool> ImportPowerPoint(string[] files, FormMain formMain)
        {
            try
            {
                Dictionary<DsSystem, PouGen> dicCpu = new Dictionary<DsSystem, PouGen>();
                var _PPTResults = ImportPPT.GetLoadingAllSystem(files);
                var storages = new Storages();
                int cnt = 0;
                await formMain.DoAsync(async tsc =>
                {
                    foreach (var ppt in _PPTResults)
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
                    }

                    PcControl.DicPou = dicCpu;
                    await PcControl.CreateRunCpuSingle();


                    formMain.Ace_Model.Expanded = true;
                    formMain.Ace_System.Expanded = true;
                    formMain.Ace_Device.Expanded = false;
                    DsProcessEvent.DoWork(100);
                    tsc.SetResult(true);
                });
                return true;
            }
            catch (Exception ex) { Global.Logger.Error(ex.Message); return false; }
        }


    }
}


