using DevExpress.Utils;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors.Controls;
using Dual.Common.Core.FS;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {
        Dictionary<DsSystem, DsCPU> _DicCpu = new Dictionary<DsSystem, DsCPU>(); 
        internal void ImportPowerPoint(string[] paths)
        {
            try
            {
                clearModel();

                var _PPTResults = ImportPPT.GetLoadingAllSystem(paths);
                var storages = new Storages();
                int cnt = 0;
                foreach (var ppt in _PPTResults)
                {
                    ProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0) / (_PPTResults.Count() * 1.0) * 100));
                    if (ppt.IsActive)
                    {
                        var pous = Cpu.LoadStatements(ppt.System, storages);
                        foreach (var pou in pous)
                            _DicCpu.Add(pou.ToSystem(), new DsCPU(pou.CommentedStatements(), pou.ToSystem()));

                        CreateHMI(ppt.System);

                    }

                    var ele = new AccordionControlElement()
                    { Style = ElementStyle.Group, Text = ppt.System.Name };

                    if (ppt.IsActive)
                    {
                        ace_System.Elements.Add(ele);
                        AppandFlows(ppt, ele);
                    }
                    else
                    {
                        ace_Device.Elements.Add(ele);
                        AppandFlows(ppt, ele);
                    }
                }




                Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
            }
            catch (Exception ex)
            {
                Global.Logger.Error(ex.Message);        
            }
            finally { ProcessEvent.DoWork(100); }
        }

  

        private void clearModel()
        {
            foreach (var item in _DicCpu.Values)
                item.Dispose();
            _DicCpu = new Dictionary<DsSystem, DsCPU>();


            tabbedView1.Controller.CloseAll();
            tabbedView1.Documents.Clear();

            clearSubElements(ace_System);
            clearSubElements(ace_Device);
            clearSubElements(ace_HMI);
            

            void clearSubElements(AccordionControlElement ele)
            {
                foreach (var subEle in ele.Elements)
                    clearSubElements(subEle);
                ele.Elements.Clear();
            }
        }

        private void AppandFlows(PptResult ppt, AccordionControlElement ele)
        {
            foreach (var v in ppt.Views)
            {
                if (v.ViewType != InterfaceClass.ViewType.VFLOW) continue;

                var eleFlow = new AccordionControlElement()
                { Style = ElementStyle.Item, Text = v.Flow.Value.Name, Tag = v };
                eleFlow.Click += (s, e) => { CreateDocOrSelect(((AccordionControlElement)s).Tag as ViewNode); };
                ele.Elements.Add(eleFlow);
            }
        }

    }
}