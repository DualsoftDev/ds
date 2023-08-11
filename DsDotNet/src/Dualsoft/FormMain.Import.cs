using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using Engine.Common.FS;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

namespace Dualsoft
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {

        internal void ImportPowerPoint(string[] paths)
        {
            try
            {
                ProcessEvent.DoWork(50);
                var _PPTResults = ImportPPT.GetLoadingAllSystem(paths);
                ProcessEvent.DoWork(100);

            
                foreach (var ppt in _PPTResults)
                {
                    var ele = new AccordionControlElement()
                    { Style = ElementStyle.Group, Text = ppt.System.Name};

                    if (ppt.IsActive)
                    {
                        accordionControlElement_System.Elements.Add(ele);
                        appendFlows(ppt, ele);
                    }
                    else
                    {
                        accordionControlElement_Device.Elements.Add(ele);
                        appendFlows(ppt, ele);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { ProcessEvent.DoWork(100); }     
        }

        private void appendFlows(PptResult ppt, AccordionControlElement ele)
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