using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Dual.Common.Core.FS;
using System;
using System.Collections.Generic;
using System.Linq;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Cpu.RunTime;
using static Engine.Cpu.RunTimeUtil;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public static class PPT
    {

        public static Dictionary<DsSystem, DsCPU> ImportPowerPoint(string[] files
            , FormMain formMain
            , TabbedView tab
            , AccordionControlElement ace_Model
            , AccordionControlElement ace_System
            , AccordionControlElement ace_Device
            , AccordionControlElement ace_HMI)
        {
            Dictionary<DsSystem, DsCPU> dicCpu = new Dictionary<DsSystem, DsCPU>();

            var _PPTResults = ImportPPT.GetLoadingAllSystem(files);
            var storages = new Storages();
            int cnt = 0;
            foreach (var ppt in _PPTResults)
            {
                ProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / (_PPTResults.Count() * 1.0) * 100));
                if (ppt.IsActive)
                {
                    var pous = Cpu.LoadStatements(ppt.System, storages);
                    foreach (var pou in pous)
                        dicCpu.Add(pou.ToSystem()
                            , new DsCPU(pou.CommentedStatements()
                            , pou.ToSystem()
                            , Global.CpuRunMode));

                    HMITree.CreateHMIBtn(formMain, ace_HMI, ppt.System);
                    Global.ActiveSys = ppt.System;
                }

                var ele = new AccordionControlElement()
                { Style = ElementStyle.Group, Text = ppt.System.Name, Tag = ppt.System };
                ele.Click += (s, e) =>
                {
                    formMain.PropertyGrid.SelectedObject = ((AccordionControlElement)s).Tag;
                };

                if (ppt.IsActive)
                    ace_System.Elements.Add(ele);
                else
                    ace_Device.Elements.Add(ele);

                var lstFlowAce = Tree.ModelTree.AppandFlows(formMain, ppt, ele);
                lstFlowAce.ForEach(f =>
                    f.Click += (s, e) =>
                    {
                        var viewNode = ((AccordionControlElement)s).Tag as ViewNode;
                        DocControl.CreateDocOrSelect(formMain, tab, viewNode);
                    });
            }

            SIMControl.RunCpus = SIMControl.GetRunCpus(dicCpu);

            ace_Model.Expanded = true;
            ace_System.Expanded = true;
            ace_Device.Expanded = false;
            ProcessEvent.DoWork(100);

            return dicCpu;
        }
    }
}


