
namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class PPT
{

    public static async Task<Dictionary<DsSystem, PouGen>> ImportPowerPoint(string[] files, FormMain formMain)
    {
        Dictionary<DsSystem, PouGen> dicCpu = new();
        Tuple<ModelLoaderModule.Model, IEnumerable<PptResult>> ret = ImportPPT.GetLoadingAllSystem(files);
        formMain.Model = ret.Item1;
        IEnumerable<PptResult> pptResults = ret.Item2;
        Storages storages = new();
        int cnt = 0;

        List<string> recentDocs = RecentDocs.GetRegistryRecentDocs();
        DsSystem activeSys = pptResults.First(f => f.IsActive).System;
        Global.ActiveSys = activeSys;


        List<PouGen> pous = Cpu.LoadStatements(activeSys, storages).ToList();
        Dictionary<Flow, ViewModule.ViewNode> viewAll = pptResults.SelectMany(f => f.Views)
                                .Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                                //.Where(w => w.UsedViewNodes.Any())
                                .ToDictionary(s => s.Flow.Value, ss => ss);
        ModelTree.CreateActiveSystemBtn(formMain, activeSys, viewAll);

        foreach (PouGen pou in pous)
        {
            DsSystem sys = pou.ToSystem();
            IEnumerable<ViewModule.ViewNode> viewSet = pptResults.First(f => f.System == sys).Views;

            dicCpu.Add(sys, pou);

            if (activeSys == sys)
            {
                await HMITree.CreateHMIBtn(formMain, sys, viewSet);
            }

            ViewDraw.DrawInitStatus(formMain.TabbedView, dicCpu);
            ViewDraw.DrawInitActionTask(formMain, dicCpu);

            IEnumerable<ViewModule.ViewNode> nodeFlows = viewSet.Where(w => w.ViewType == InterfaceClass.ViewType.VFLOW)
                           .Where(w => w.UsedViewNodes.Any())
                           .Where(w => recentDocs.Contains(w.Flow.Value.QualifiedName));

            _ = nodeFlows.Iter(f => DocContr.CreateDocOrSelect(formMain, f));

            DsProcessEvent.DoWork(Convert.ToInt32(cnt++ * 1.0 / pous.Count() * 50));
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


