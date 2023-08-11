using DevExpress.XtraEditors;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Engine.Common;
using static Model.Import.Office.ImportPPTModule;
using DevExpress.XtraBars.Docking2010.Views;

namespace Dualsoft
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {
     

        private void CreateDocOrSelect(PptResult pptResult)
        {
            string docKey = pptResult.System.Name;
            BaseDocument document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
            if (document != null) tabbedView1.Controller.Activate(document);
            else
            {
                //UCTaskUI_Form form = new UCTaskUI_Form();
                //form.Name = docKey;
                //form.MdiParent = this;
                //form.Text = docKey;
                //form.Show();
                document = tabbedView1.Documents.Where(w => w.Control.Name == docKey).FirstOrDefault();
                document.Caption = docKey;
            }



        }


    }
}