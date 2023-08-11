using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using Engine.Common.FS;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Model.Import.Office.ImportPPTModule;

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

            
                foreach (var view in _PPTResults)
                {
                    var ele = new AccordionControlElement()
                    { Style = ElementStyle.Item, Text = view.System.Name, Tag = view};

                    ele.Click += (s, e) => { CreateDocOrSelect(((AccordionControlElement)s).Tag as PptResult); };

                    if (view.IsActive)
                        accordionControlElement_System.Elements.Add(ele);
                    else
                        accordionControlElement_Device.Elements.Add(ele);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally { ProcessEvent.DoWork(100); }     
        }

    
    }
}